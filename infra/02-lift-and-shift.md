# Stage 2: Lift and shift the old app to App Service

Goal: run the old app, unchanged, on App Service (Windows) against Azure SQL. Only hosting and config change.

Run from the repo root:
```bash
source infra/env.sh
```

## 1. Build the package

```bash
./scripts/package-legacy.sh
```
Why: compiles the unchanged legacy code for .NET Framework 4.8 on the Mac and zips it in the same layout Visual Studio's "Publish" would make (`bin/`, `Views/`, `Content/`, `Global.asax`, `Web.config`). On Windows you could publish from Visual Studio instead.

## 2. Point the app at Azure SQL (config only)

```bash
echo ${#SQL_ADMIN_PASSWORD}     # must not be 0; if it is, run the next line
read -s -p "SQL admin password: " SQL_ADMIN_PASSWORD; echo

az webapp config connection-string set -g $RG -n $LEGACY_APP -t SQLAzure --settings \
  ReadingsDb="Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;MultipleActiveResultSets=True" \
  -o none
```
Why: App Service replaces the `ReadingsDb` entry from `Web.config` at runtime with this one, so no code or file changes. `-o none` stops the CLI printing the password back.

## 3. Deploy and open

```bash
az webapp deploy -g $RG -n $LEGACY_APP --src-path legacy/publish.zip --type zip
open https://$LEGACY_APP.azurewebsites.net
```
Why: zip deploy copies the package into the site. The first request takes 20 to 40 seconds while EF6 creates the `Readings` table, seeds 5 readings and the views compile.

## Acceptance (also the Stage 0 check)

- The list shows 5 sample readings (MTR-001 and MTR-002).
- Submit `MTR-001`, value `1500`, today: saved and shown at the top.
- Submit `MTR-001`, value `10`: rejected with "lower than the previous reading".
- A future date or a negative value: rejected with a clear message.

## The problem this leaves (fixed in Stage 3)

The app works, but the database password now sits in plain text in the App Service configuration (anyone with read access to the web app sees it), and it is the **server admin** password. Stage 3 removes it with managed identity and Key Vault.

## If something fails

- **Yellow error page or HTTP 500**: turn on detailed errors and stream the log:
  `az webapp config set -g $RG -n $LEGACY_APP --detailed-error-logging-enabled true` then `az webapp log tail -g $RG -n $LEGACY_APP`.
  Or temporarily set `<customErrors mode="Off" />` in `Web.config`, rebuild and redeploy.
- **Login failed for user**: the password in step 2 is wrong. Rerun step 2.
- **Password contains `;`**: wrap it in braces in the connection string, `Password={your;password}`.
