#!/usr/bin/env bash
# Publishes the new app and zips it for App Service. Output: modern/publish.zip (overwritten each run)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ZIP="$ROOT/modern/publish.zip"
STAGE="$(mktemp -d)"
dotnet publish "$ROOT/modern/ReadingsApp.Web/ReadingsApp.Web.csproj" -c Release -nologo -v q -o "$STAGE"
rm -f "$ZIP"
(cd "$STAGE" && zip -qr "$ZIP" .)
echo "Package ready: $ZIP"
