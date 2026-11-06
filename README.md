# Meter Readings demo

A tiny app that shows an old ASP.NET MVC 5 app moving to Azure PaaS and then to .NET 10, in small safe steps.
Two features only: **submit a reading** and **view readings** (newest first, filter by Meter ID).

| URL | What |
|---|---|
| https://app-readings-web-jacreator2.azurewebsites.net | The new app (.NET 10, passwordless). `/health` checks the database |
| https://app-readings-legacy-jacreator2.azurewebsites.net | The old app (.NET Framework 4.8), kept until after the demo |

## Repository

| Folder | What it is |
|---|---|
| `shared/ReadingsApp.Core` | .NET Standard 2.0: models, `ReadingService` (all rules), `IReadingRepository`. Used by both apps |
| `legacy/ReadingsApp.Legacy` | Old app: ASP.NET MVC 5, .NET Framework 4.8, EF6 |
| `modern/ReadingsApp.Web` | New app: ASP.NET Core MVC, .NET 10, EF Core, managed identity, health check, OpenTelemetry |
| `modern/ReadingsApp.Tests` | xUnit tests for the five rules |
| `infra/` | Azure CLI steps per stage (`01` to `06`), shared names in `env.sh`, SQL scripts |
| `scripts/` | Build and zip each app for App Service, grant a managed identity SQL access |
| `.github/workflows/deploy.yml` | Pipeline: test, deploy to staging, check `/health`, swap (OIDC) |
| `docs/` | `stage-notes.md`, `upgrade-log.md`, `demo-script.md` |

## Run

**Tests** (any OS): `dotnet test modern/ReadingsApp.Tests`

**New app locally** (any OS, .NET 10 SDK): `az login`, add a SQL firewall rule for your IP, make sure your Entra account has access to `ReadingsDb`, then `dotnet run --project modern/ReadingsApp.Web`. The same connection string works locally and on Azure because `Active Directory Default` uses your `az login` here and the managed identity there.

**Old app locally** (Windows only: Visual Studio 2022 and SQL Server Express):
1. `sqlcmd -S localhost\SQLEXPRESS -E -i legacy\db\setup-local.sql` (Windows sign-in, no password)
2. Open `legacy\ReadingsApp.Legacy.sln`, press F5 (`http://localhost:51080/`). EF6 creates the table and 5 sample readings.

**Deploy:** `./scripts/package-web.sh` or `./scripts/package-legacy.sh`, then `az webapp deploy` to the staging slot and swap (see `infra/`). The old app also builds on macOS through `legacy/build-on-mac`.

## Azure resources

Resource group `rg-readings-demo`, region Spain Central (Azure for Students only allows a few regions).

| Resource | Name | Purpose |
|---|---|---|
| Azure SQL server | `sql-readings-jacreator2` | Entra admin. SQL logins switched off after the demo |
| Azure SQL database | `ReadingsDb` (Basic) | The one shared database |
| App Service plan | `plan-readings-demo` (Windows, S1) | S1 so deployment slots work |
| Web app + `staging` slot | `app-readings-web-jacreator2` | New app, .NET 10, managed identity to SQL |
| Web app + `staging` slot | `app-readings-legacy-jacreator2` | Old app, deleted after the demo |
| Log Analytics workspace | `log-readings-demo` | Storage for Application Insights |
| Application Insights | `appi-readings-demo` | Telemetry from the new app |
| Key Vault | `kv-readings-jacreator2` (RBAC) | Held the old app's connection string |
| User-assigned managed identity | `id-github-deploy` | GitHub Actions signs in as this (OIDC, `master` only), can deploy and swap the new app only |

Remove everything when finished: `az group delete -n rg-readings-demo`.
