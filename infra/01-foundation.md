# Stage 1: Azure foundation

Goal: create every Azure resource the demo needs, in one resource group `rg-readings-demo` in Spain Central (`$LOC`).
You run each command yourself. Each one has a one-line reason.

**Cost:** the S1 App Service plan is the main cost (roughly £55 a month while it exists). Azure SQL Basic is a few pounds a month.
Delete the resource group at the end of the demo (`az group delete -n rg-readings-demo`) to stop all charges.

## 0. Before you start

```bash
az login                                   # sign in through the browser
az account show -o table                   # check you are on the right subscription
az account set -s "<subscription name or id>"   # only if it is the wrong one
source infra/env.sh                        # load the shared names (edit SUFFIX first)
```

## 0b. Register the resource providers (new subscriptions only)

```bash
for ns in Microsoft.Sql Microsoft.Web Microsoft.KeyVault Microsoft.Insights Microsoft.OperationalInsights; do
  az provider register -n $ns
done
az provider list --query "[?contains('Microsoft.Sql Microsoft.Web Microsoft.KeyVault Microsoft.Insights Microsoft.OperationalInsights', namespace)].{ns:namespace, state:registrationState}" -o table
```
Why: a new subscription has to switch on each Azure service before first use. Rerun the last line until every row says `Registered` (usually 1 to 3 minutes).

## 1. Resource group

```bash
az group create -n $RG -l $LOC
```
Why: one container for everything, so it is easy to see and easy to delete.

## 2. Azure SQL (Entra admin plus a SQL login, option A)

```bash
read -s -p "SQL admin password: " SQL_ADMIN_PASSWORD; echo
```
Why: you type the password at a hidden prompt, so it never lands in a file or your shell history.

```bash
az sql server create -g $RG -n $SQL_SERVER -l $LOC \
  --admin-user $SQL_ADMIN_USER --admin-password "$SQL_ADMIN_PASSWORD"
```
Why: the logical SQL server. The SQL login stays on for now because Stage 2 uses it (the problem we fix in Stage 3).

```bash
az sql server ad-admin create -g $RG -s $SQL_SERVER \
  --display-name "$(az ad signed-in-user show --query userPrincipalName -o tsv)" \
  --object-id "$(az ad signed-in-user show --query id -o tsv)"
```
Why: makes you the Entra admin, which Stage 3 and 5 need to grant the app's managed identity access without a password.

```bash
az sql server firewall-rule create -g $RG -s $SQL_SERVER -n AllowAzureServices \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```
Why: lets App Service reach the database. (0.0.0.0 is Azure's special value for "Azure services only", not the whole internet.)

```bash
az sql db create -g $RG -s $SQL_SERVER -n $SQL_DB --service-objective Basic
```
Why: the empty `ReadingsDb` database. The app creates the `Readings` table on first run. Basic is the cheapest tier and is plenty for a demo.

## 3. App Service plan and the old app

```bash
az appservice plan create -g $RG -n $PLAN -l $LOC --sku S1 --is-linux false
```
Why: Windows hosting for the .NET Framework app (newer CLI versions default to Linux, hence `--is-linux false`). S1 is the lowest tier with deployment slots, which Stage 3 needs.

```bash
az webapp create -g $RG -p $PLAN -n $LEGACY_APP --runtime "ASPNET:V4.8"
```
Why: the empty web app that the old app is published into in Stage 2.

## 4. Monitoring

```bash
az monitor log-analytics workspace create -g $RG -n $LOG_WS -l $LOC
```
Why: Application Insights stores its data in a Log Analytics workspace.

```bash
az monitor app-insights component create -g $RG -a $APPI -l $LOC \
  --workspace "$(az monitor log-analytics workspace show -g $RG -n $LOG_WS --query id -o tsv)"
```
Why: the telemetry target for Stage 5. (If the CLI asks to install the `application-insights` extension, say yes.)

## 5. Key Vault

```bash
az keyvault create -g $RG -n $KV -l $LOC --enable-rbac-authorization true
```
Why: where the connection string moves to in Stage 3. RBAC mode means access is granted with Azure roles (such as `Key Vault Secrets User`), not vault access policies.

## 6. Check

```bash
az resource list -g $RG -o table
```
Acceptance: you should see the SQL server, the database, the plan, the web app, the Log Analytics workspace, Application Insights and the Key Vault.

## If something fails

- **"The subscription is not registered to use namespace ..."**: run `az provider register -n Microsoft.Sql` (or `Microsoft.Web`, `Microsoft.KeyVault`, `Microsoft.Insights`, `Microsoft.OperationalInsights`), wait a minute and retry.
- **"RequestDisallowedByAzure ... best available regions"**: your subscription (for example Azure for Students) only allows certain regions. List them with `az policy assignment list --disable-scope-strict-match --query "[].parameters.listOfAllowedLocations.value" -o tsv`, set `LOC` in `infra/env.sh` to one of them, `source` it again and rerun the failed step.
- **"... name is already in use"**: change `SUFFIX` in `infra/env.sh`, `source` it again and rerun the failed step.
- **"Location ... is not accepting creation of new ... SQL Database servers"**: that region is short of capacity. Check which allowed regions accept SQL with
  `az rest --method get --url "https://management.azure.com/subscriptions/$(az account show --query id -o tsv)/providers/Microsoft.Sql/locations/<region>/capabilities?api-version=2021-11-01" --query status -o tsv`
  (`Available` is good, `Visible` means blocked), then change `LOC` in `infra/env.sh`.
- **"The Resource group already exists in location ..."**: harmless. A resource group's location cannot change, and resources inside it can live in another region. Skip step 1.
