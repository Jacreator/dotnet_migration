# Stage notes

## Stage 0: the old app

**What:** `legacy/ReadingsApp.Legacy`, an ASP.NET MVC 5 app on .NET Framework 4.8 with EF6 code first and SQL Server.

**Shape** (same names will be used in the new app):
- `Controllers/ReadingsController`: thin. `Index` lists, `Create` shows and posts the form.
- `Services/ReadingService`: the only place with the rules (Meter ID required and max 20, value zero or more, date not in the future, not lower than the previous reading for that meter).
- `Data/ReadingRepository`: EF6 access. `ReadingsDbContext` defines the `Readings` table and the index on `MeterId, ReadingDate`.
- `ReadingsControllerFactory`: MVC 5 has no built-in DI, so this small class is the composition root. It builds one `DbContext` per request and injects interfaces through constructors. No DI library needed.

**Choices worth knowing:**
- `ReadingInput` only has `[Display]` attributes. All validation is in `ReadingService` so the rules live in one place (the brief asks for both; one place wins).
- EF6 automatic migrations (no generated migration files) create the table on first run. This also works later against an existing empty Azure SQL database, which plain `CreateDatabaseIfNotExists` does not.
- The connection string has a SQL username and password in `Web.config`. This is deliberate: it is the problem Stage 3 fixes.
- No jQuery, Bootstrap or client-side validation. Server-side validation only.

**Checked on macOS:** the C# compiles against .NET Framework 4.8 reference assemblies with zero warnings, and the five service rules pass with an in-memory repository. Views and IIS Express must be checked on Windows.

## Stage 1: Azure foundation

**What:** all Azure resources created with Azure CLI from `infra/01-foundation.md`. Resource list is in `README.md`.

**What we hit, and why it matters for anyone repeating this:**
- Azure for Students only allows five regions (Spain Central, Norway East, Italy North, Denmark East, Switzerland North). UK South was refused.
- Switzerland North and Norway East refuse new SQL servers (capacity). Spain Central accepts SQL and S1, so everything lives there.
- A refused SQL create still holds the server name for a while, so the name suffix changed to `jacreator2`.
- New subscriptions must register resource providers (`Microsoft.Sql`, `Microsoft.Web`, ...) before first use.
- Azure CLI 2.92 creates Linux App Service plans by default. .NET Framework needs Windows, so the plan needs `--is-linux false`.

**Choice:** Azure SQL has an Entra admin and a SQL login (option A). Stage 2 uses the SQL login on purpose so Stage 3 has a real password to remove.

## Stage 2: lift and shift to App Service

**What:** the old app runs unchanged on App Service (Windows, .NET Framework 4.8) at `https://app-readings-legacy-jacreator2.azurewebsites.net`, against Azure SQL `ReadingsDb`. Steps in `infra/02-lift-and-shift.md`.

**What changed:** hosting and config only. The `ReadingsDb` connection string is set as an App Service connection string, which overrides the `Web.config` entry at runtime. No code changed. EF6 created the table and seed data in the empty Azure database on first request.

**Built on a Mac:** `scripts/package-legacy.sh` compiles the unchanged source for .NET Framework 4.8 with the .NET SDK (`legacy/build-on-mac`) and zips it in the same layout Visual Studio's Publish makes. App Service compiles the Razor views on first use. This was also the first real run of the old app, so it doubles as the Stage 0 acceptance.

**The problem left behind:** the SQL **server admin** password sits in plain text in the App Service configuration (and a local dev password in `Web.config`). Stage 3 removes it.

## Stage 3: secrets and identity

**What:** no password in `Web.config` (local runs use Windows sign-in). On Azure the connection string is a Key Vault reference, read by each slot's system-assigned managed identity with `Key Vault Secrets User`. The server admin login was replaced by `readings_app`, a contained database user with read and write only. Released through a staging slot with swap, rollback and swap again. Steps: `infra/03-secrets-and-identity.md`.

**What we hit:** zsh does not split `$VAR` into words, so a loop meant for both slots never gave staging an identity (fixed: two plain commands). A new role assignment can take minutes, and App Service keeps a failed Key Vault lookup until the setting is saved again or the app restarts. A mobile network changes the public IP between connections, so the temporary SQL firewall rule needed a small range.

**Still not ideal:** the password exists, locked in Key Vault, because the old app's `System.Data.SqlClient` cannot use a managed identity. Removed after the demo (`infra/05-after-demo.md`).

## Stage 4: upgrade in small moves

See `docs/upgrade-log.md`. Result: `app-readings-web-jacreator2` serves both features on .NET 10 with EF Core, passwordless to SQL.

## Stage 5: finish the new app on PaaS

**What:** `/health` (checks the database with `AddDbContextCheck`) is the App Service health check path. Application Insights through OpenTelemetry (`UseAzureMonitor`), switched on by the `APPLICATIONINSIGHTS_CONNECTION_STRING` app setting. YARP removed. Deployed to a staging slot (own identity, own database user), checked `/health` and the smoke test, then swapped.

**Not done yet, on purpose:** deleting the old App Service, switching SQL to Entra-only authentication and deleting the Key Vault secret. These would break the old app used in the demo. Commands: `infra/05-after-demo.md`.

**Key Vault in the new app:** not used. With managed identity to SQL there is no secret left to store, so a Key Vault config provider would be unused code. The vault stays for any future secret.

## Stage 6: pipeline

**What:** `.github/workflows/deploy.yml` (test, publish, deploy to staging, check `/health`, swap) signing in with OpenID Connect. Setup in `infra/06-pipeline.md`. Not run yet: needs a GitHub repository.
