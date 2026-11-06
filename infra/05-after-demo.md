# Stage 5 (final steps): switch off the old app and every password

Run these **after the demo**. Until then the old app stays up so the slot swap demo still works.
The new app (`app-readings-web-jacreator2`) already serves both features on .NET 10 and does not depend on the old app.

```bash
source infra/env.sh
```

## 1. Delete the old App Service (and its staging slot)

```bash
az webapp delete -g $RG -n $LEGACY_APP
```
Why: the new app handles both features, so nothing routes to the old app any more.

## 2. Switch SQL to Entra-only authentication

```bash
az sql server ad-only-auth enable -g $RG -n $SQL_SERVER
```
Why: turns off every SQL password login, including `readingsadmin` and `readings_app`. Only Entra identities (you and the new app's managed identities) can sign in. Do this only after step 1, because the old app uses a password.

## 3. Remove what only the old app needed

```bash
az keyvault secret delete --vault-name $KV -n ReadingsDb
```
Why: the last copy of a database password. It no longer works after step 2 anyway.

Optional, from a machine with a firewall rule and `sqlcmd` (see `infra/03-secrets-and-identity.md`):
```bash
sqlcmd -S $SQL_SERVER.database.windows.net -d $SQL_DB --authentication-method ActiveDirectoryDefault -Q "DROP USER readings_app"
```

## Check

```bash
az resource list -g $RG -o table                        # no app-readings-legacy-*
az sql server ad-only-auth get -g $RG -n $SQL_SERVER     # azureAdOnlyAuthentication: true
curl https://$WEB_APP.azurewebsites.net/health           # Healthy
```
