# Stage 3: Secrets and identity

Goal: no password in `Web.config` or in plain App Service settings. The connection string lives in Key Vault,
the web app reads it with its managed identity, and the change goes out through a staging slot with a swap and a rollback.

What is already done in the code: `Web.config` has no password (local runs use Windows sign-in), and the footer shows a
`ReleaseLabel` so you can see which version is live during the swap.

Run from the repo root:
```bash
source infra/env.sh
KV_ID=$(az keyvault show -n $KV --query id -o tsv)
ME=$(az ad signed-in-user show --query id -o tsv)
```

## 1. A least-privilege database user (replaces the server admin)

```bash
brew install sqlcmd
```
Why: the modern `sqlcmd` can sign in to Azure SQL with your Entra account (the Entra admin from Stage 1), so no password is typed.

```bash
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create -g $RG -s $SQL_SERVER -n TempMyMac --start-ip-address $MY_IP --end-ip-address $MY_IP -o none
```
Why: lets your Mac reach the database for the next command. Removed again in step 6.

On a mobile network or hotspot your public IP can change between connections (sqlcmd then reports a different
"Client with IP address"). Allow the small range instead, for example
`--start-ip-address 92.40.190.0 --end-ip-address 92.40.190.255`, using the first three numbers from the error.

```bash
APP_DB_PASSWORD="$(openssl rand -base64 24 | tr -d '/+=')Aa1"
sqlcmd -S $SQL_SERVER.database.windows.net -d $SQL_DB --authentication-method ActiveDirectoryDefault \
  -i infra/sql/create-app-user.sql -v AppPassword="$APP_DB_PASSWORD"
```
Why: creates `readings_app`, a user that exists only in `ReadingsDb` and can only read and write data. The password is random and nobody needs to know it, because it goes straight into Key Vault next.

## 2. Put the connection string in Key Vault

```bash
az role assignment create --assignee-object-id $ME --assignee-principal-type User \
  --role "Key Vault Secrets Officer" --scope $KV_ID -o none
```
Why: the vault uses Azure roles, so even the subscription owner needs a role to write secrets. Wait about a minute for it to take effect.

```bash
az keyvault secret set --vault-name $KV -n ReadingsDb -o none --value \
  "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User ID=readings_app;Password=$APP_DB_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;MultipleActiveResultSets=True"
unset APP_DB_PASSWORD
```
Why: the only copy of the password is now in the vault. `unset` clears it from your shell.

## 3. Managed identities that can read the secret

```bash
az webapp deployment slot create -g $RG -n $LEGACY_APP -s staging --configuration-source $LEGACY_APP -o none
```
Why: a staging copy of the site (S1 allows slots) that starts with production's settings.

```bash
# production slot
PRINCIPAL=$(az webapp identity assign -g $RG -n $LEGACY_APP --query principalId -o tsv)
az role assignment create --assignee-object-id $PRINCIPAL --assignee-principal-type ServicePrincipal \
  --role "Key Vault Secrets User" --scope $KV_ID -o none

# staging slot
PRINCIPAL=$(az webapp identity assign -g $RG -n $LEGACY_APP -s staging --query principalId -o tsv)
az role assignment create --assignee-object-id $PRINCIPAL --assignee-principal-type ServicePrincipal \
  --role "Key Vault Secrets User" --scope $KV_ID -o none
```
Why: turns on a system-assigned managed identity for each slot and lets it read secrets (read only). Each slot has its own identity, and identities do not move during a swap, so both need the role.

## 4. Deploy to staging with the Key Vault reference

```bash
az webapp config connection-string set -g $RG -n $LEGACY_APP -s staging -t SQLAzure -o none \
  --settings ReadingsDb="@Microsoft.KeyVault(VaultName=$KV;SecretName=ReadingsDb)"
```
Why: staging's setting now holds a pointer, not a password. App Service fetches the value with the slot's identity.

```bash
./scripts/package-legacy.sh
az webapp deploy -g $RG -n $LEGACY_APP --slot staging --src-path legacy/publish.zip --type zip
open https://$LEGACY_APP-staging.azurewebsites.net
```
Why: deploys the new build (no password in `Web.config`, footer label) to staging only. Production is untouched.

Check staging: the list shows your readings and the footer says `Release: Stage 3: secrets in Key Vault`. Submit one reading to prove writes work with `readings_app`.

If the page errors with a login failure, check the reference resolved:
```bash
az rest --method get --url "https://management.azure.com$(az webapp show -g $RG -n $LEGACY_APP -s staging --query id -o tsv)/config/configreferences/connectionstrings?api-version=2022-03-01" \
  --query "value[].{name:name, status:properties.status}" -o table
```
`Resolved` is good. Anything else usually means the role from step 3 has not taken effect yet: wait a minute and restart the slot (`az webapp restart -g $RG -n $LEGACY_APP -s staging`).

## 5. Swap, roll back, swap again

```bash
az webapp deployment slot swap -g $RG -n $LEGACY_APP -s staging --target-slot production
open https://$LEGACY_APP.azurewebsites.net
```
Why: production now runs the new build with the Key Vault reference. The footer shows the release label.

```bash
az webapp deployment slot swap -g $RG -n $LEGACY_APP -s staging --target-slot production
```
Why: the rollback. Production is back to the old build (no footer) in seconds, with nothing rebuilt or redeployed. Refresh to see it.

```bash
az webapp deployment slot swap -g $RG -n $LEGACY_APP -s staging --target-slot production
```
Why: roll forward again so production stays on the new build.

## 6. Tidy up

```bash
az webapp config connection-string set -g $RG -n $LEGACY_APP -s staging -t SQLAzure -o none \
  --settings ReadingsDb="@Microsoft.KeyVault(VaultName=$KV;SecretName=ReadingsDb)"
az sql server firewall-rule delete -g $RG -s $SQL_SERVER -n TempMyMac
```
Why: after the swaps, staging holds the old admin-password setting. Replace it with the reference too. Then close the firewall hole for your Mac.

## Acceptance

```bash
az webapp config connection-string list -g $RG -n $LEGACY_APP --query "[].value" -o tsv
az webapp config connection-string list -g $RG -n $LEGACY_APP -s staging --query "[].value" -o tsv
```
Both print `@Microsoft.KeyVault(VaultName=...;SecretName=ReadingsDb)`, with no password. `Web.config` has no password. The swap and rollback worked.

## What is still not ideal (fixed in Stage 5)

The password still exists, it is just locked in Key Vault. SQL password logins stay switched on because the old app's driver
(`System.Data.SqlClient`) cannot sign in with a managed identity. Stage 5 connects the new app with its managed identity and switches password logins off.
