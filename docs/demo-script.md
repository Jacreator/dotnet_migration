# Demo script: Meter Readings, from legacy to Azure PaaS

Five minutes. Shows the old app, the lift to PaaS, secrets and slots, and then the finished .NET 10 app, all live.

## The night before (about 20 minutes)

Run from the repo root after `source infra/env.sh`.

Already done: production of the old app runs the Key Vault build (footer "Stage 3: secrets in Key Vault"), its staging slot holds "Release 2 (deployed through the staging slot)", and Always On is on for every slot.

1. **Check both old-app URLs** show those two footers.

2. **Record a backup.** Screen-record yourself doing the whole run once (QuickTime > File > New Screen Recording). If the venue Wi-Fi or your hotspot fails, play the recording and talk over it.

## 30 minutes before

- `az login` and `source infra/env.sh` in a terminal with a large font. Type the swap command but do not press Enter:
  `az webapp deployment slot swap -g $RG -n $LEGACY_APP -s staging --target-slot production`
- Browser tabs, in this order:
  1. The old app: `https://app-readings-legacy-jacreator2.azurewebsites.net`
  2. Portal: resource group `rg-readings-demo` (Overview)
  3. Portal: web app > Settings > Environment variables > **Connection strings** (shows the Key Vault reference with a green tick)
  4. Portal: web app > **Deployment slots**
  5. Portal: Key Vault > **Access control (IAM)** > Role assignments
  6. The new app: `https://app-readings-web-jacreator2.azurewebsites.net`
  7. The new app's health: `https://app-readings-web-jacreator2.azurewebsites.net/health`
  8. Portal: Application Insights `appi-readings-demo` > **Application map** (or Live metrics)
- VS Code with these files open: `shared/ReadingsApp.Core/Services/ReadingService.cs` (shared by both apps), `legacy/ReadingsApp.Legacy/Web.config`, `infra/`.
- Load the app once so it is warm.
- **Never click "Show Secret Value" in Key Vault, and never run `az keyvault secret show`, with the screen shared.**

## Talk track

### 0:00 to 0:45. The old app

**Show:** tab 1. Submit `MTR-001`, a value higher than the top reading, today's date: saved, appears at the top. Submit `MTR-001`, value `10`: rejected.

**Say:** "This stands in for our real system. It is ASP.NET MVC 5 on .NET Framework 4.8 with Entity Framework 6, the kind of app that usually lives on a Windows VM. Two features: submit a reading, view readings. The rules, such as 'a reading cannot go backwards', live in one service class." Flick to `ReadingService.cs` for two seconds.

### 0:45 to 1:45. Lift and shift to PaaS

**Show:** tab 2, the resource group.

**Say:** "Step one was to move it as it is. No code changes. It now runs on Azure App Service on Windows, against Azure SQL. No VMs to patch, no IIS to manage. The only change was hosting and config: the connection string is set in App Service instead of `Web.config`. All of this was created with a short, scripted set of Azure CLI commands that are in the repo."

**Say:** "But at this point we still had the classic problem: the database password, the server admin one, sat in plain text in config. Anyone who could read the app settings could read the password."

### 1:45 to 3:30. Secrets, identity and slots

**Show:** `Web.config` in VS Code. "No password in the code any more."

**Show:** tab 3. "The connection string is now a Key Vault reference. The app has its own managed identity, an Azure identity with no password, and that identity is allowed to read secrets from this vault and nothing else." Tab 5: point at `Key Vault Secrets User`.

**Say:** "We also replaced the server admin with a database user that can only read and write data in this one database."

**Show:** tab 4, deployment slots. "We never deploy straight to production. We deploy to a staging slot, check it, then swap."

**Do:** press Enter on the swap command. While it runs (about a minute): "A swap warms the new version up in staging first, then switches traffic over. No downtime, nothing rebuilt." When it finishes, refresh tab 1: the footer says **Release 2**.

**Say:** "And if something is wrong, rollback is the same command." Run the swap again, refresh: footer back to the previous release. "Seconds, not a redeploy."

### 3:30 to 4:30. Upgrade in small steps

**Say:** "Then we modernised without a big bang rewrite. Each step was deployed and checked:
1. The models and rules moved into a .NET Standard library that both apps share, so the rules exist once.
2. A new ASP.NET Core app went in front of the old one, with a reverse proxy sending everything to the old app.
3. We rebuilt 'submit a reading' in the new app and took over just that route, then 'view readings'. Same database, so users never noticed.
4. Then we retargeted from .NET 8 to .NET 10, the long-term support release. .NET 8 support ends on 10 November. That retarget also flagged a vulnerable package, which we fixed."

**Show:** tab 6, the new app. Point at the header: ".NET 10". Submit a reading: the same rules, the same data.

### 4:30 to 5:00. Where we end up

**Show:** tab 7: `Healthy`. "App Service checks this and pulls a bad instance out." Tab 8: requests and SQL calls in Application Insights.

**Say:** "The new app connects to Azure SQL with its managed identity, so it has no password at all, not even in Key Vault. Once the old app is switched off, password logins are turned off on the server. Every release goes through a staging slot, and a GitHub Actions pipeline does test, deploy to staging, health check and swap, signing in to Azure without a stored secret."

## Likely questions

| Question | Answer |
|---|---|
| Why is there still a password in Key Vault? | Only for the old app: its SQL driver (`System.Data.SqlClient`) cannot sign in with a managed identity. The new app has no password. When the old app is deleted, the secret goes and the server becomes Entra-only. |
| Does the swap cause downtime? | No. App Service warms up the staging slot before switching traffic. |
| How fast is a rollback? | One swap, usually under a minute. Nothing is rebuilt. |
| Why not containers or Kubernetes? | App Service runs the existing app with no changes and no cluster to run. Containers can come later, after the move to .NET 10. |
| What does it cost? | Mostly the S1 App Service plan (roughly £55 a month). Azure SQL Basic is a few pounds. Everything sits in one resource group, which is deleted after the demo. |
| How do the old and new apps share data during the upgrade? | Same database, same table. The new app only takes over one route at a time. |

## If something goes wrong

- **Swap hangs or fails:** "This is why we keep a staging slot." Show tab 4 and move on. Or play the backup recording.
- **App shows an error page:** go to the recording.
- **Portal asks you to sign in again:** sign in before the demo and keep the tabs open.
