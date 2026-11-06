#!/usr/bin/env bash
# Builds the legacy app on macOS/Linux and zips it for App Service (Windows).
# Output: legacy/publish.zip (overwritten each run)
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/legacy/ReadingsApp.Legacy"
ZIP="$ROOT/legacy/publish.zip"
STAGE="$(mktemp -d)"   # fresh folder each run, outside the repo

dotnet build "$ROOT/legacy/build-on-mac/ReadingsApp.Legacy.MacBuild.csproj" -c Release -nologo -v q -o "$STAGE/bin"

# Files IIS needs next to bin/
cp "$SRC/Global.asax" "$SRC/Web.config" "$STAGE/"
cp -R "$SRC/Views" "$SRC/Content" "$STAGE/"

rm -f "$ZIP"
(cd "$STAGE" && zip -qr "$ZIP" .)
echo "Package ready: $ZIP"
