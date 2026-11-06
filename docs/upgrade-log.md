# Upgrade log (Stage 4)

Each move was deployed and checked through one public URL, `https://app-readings-web-jacreator2.azurewebsites.net`:
submit a valid reading, submit a lower one (rejected), see the new reading at the top of the list.

## Move 1: shared code into .NET Standard 2.0

- New library `shared/ReadingsApp.Core` (netstandard2.0): `Models/Reading`, `Models/ReadingInput`, `Services/IReadingService`, `Services/ReadingService`, `Data/IReadingRepository`.
- Files moved as they were. Namespaces were already `ReadingsApp.*`, so no code changed.
- The old app references the library. Its EF6 `ReadingRepository`, `DbContext`, controller and views stay in the old app.
- `ValidationResult` is not in .NET Standard 2.0, so Core uses the `System.ComponentModel.Annotations` package. It is added to the old app's `packages.config` too, because `packages.config` projects do not pick up a referenced project's packages.
- xUnit tests for the five rules (`modern/ReadingsApp.Tests`) run against Core: 5 passed.
- Not redeployed to the old App Service, to keep the demo slots unchanged. The old app builds clean against Core.

## Move 2: new ASP.NET Core app on .NET 8, proxying everything

- `modern/ReadingsApp.Web` (net8.0) with YARP. One catch-all route sends every request to the old app.
- New App Service `app-readings-web-jacreator2` on the same Windows S1 plan, with a system-assigned managed identity.
- Check: the list, the form and the CSS all came from the old app through the new URL.
- **System.Web adapters not added.** They share session and authentication between the two apps, and this app has neither. Adding them would be unused code.

## Move 3: feature 1 (Submit a reading) in the new app

- `ReadingsController.Create` (GET and POST), `Views/Readings/Create`, `_Layout`, EF Core `ReadingsDbContext` and `ReadingRepository` mapped to the existing `Readings` table.
- Same `ReadingService` from Core, so the rules are not duplicated.
- SQL access is passwordless from the start: `Authentication=Active Directory Default` (DefaultAzureCredential) and a contained database user for the managed identity (`scripts/grant-sql-identity.sh`).
- MVC routes win over the proxy because the YARP route has `Order: 1000`, so only actions that exist in the new app are taken over.
- Check: form from the new app (.NET 8.0.33), saved row visible in the old app's list.

## Move 4: feature 2 (View readings) in the new app

- `ReadingsController.Index` and `Views/Readings/Index`.
- Check: both pages from the new app. The proxy no longer serves any feature.

## Move 5: .NET 8 to .NET 10

- `TargetFramework` net8.0 to net10.0, EF Core 8.0.33 to 10.0.14, App Service runtime set to .NET 10.
- **Warning fixed:** `Microsoft.Data.SqlClient` 6.1.7 pulls in `System.Security.Cryptography.Pkcs` 9.0.11, which has known high and moderate vulnerabilities (NU1903, NU1902). Pinned `System.Security.Cryptography.Pkcs` 10.0.14. Build: 0 warnings.
- Tests: 5 passed. Check: header shows .NET 10.0.14, submit and list work.
- Why not stay on 8: .NET 8 and 9 lose support on 10 November 2026.
