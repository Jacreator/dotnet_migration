#!/usr/bin/env bash
# Grants a web app's (or slot's) managed identity read/write on ReadingsDb.
# Usage: scripts/grant-sql-identity.sh <web app name> [slot]
# Needs: source infra/env.sh, az login as the SQL Entra admin, sqlcmd, and a firewall rule for this machine.
set -euo pipefail
APP="$1"; SLOT="${2:-}"
if [ -n "$SLOT" ]; then
  PRINCIPAL=$(az webapp identity show -g "$RG" -n "$APP" -s "$SLOT" --query principalId -o tsv)
  NAME="$APP/slots/$SLOT"
else
  PRINCIPAL=$(az webapp identity show -g "$RG" -n "$APP" --query principalId -o tsv)
  NAME="$APP"
fi
APP_ID=$(az ad sp show --id "$PRINCIPAL" --query appId -o tsv)
SID="0x$(python3 -c "import uuid,sys; print(uuid.UUID(sys.argv[1]).bytes_le.hex().upper())" "$APP_ID")"
sqlcmd -S "$SQL_SERVER.database.windows.net" -d "$SQL_DB" --authentication-method ActiveDirectoryDefault \
  -i "$(dirname "$0")/../infra/sql/create-identity-user.sql" -v UserName="$NAME" Sid="$SID"
echo "Granted $NAME"
