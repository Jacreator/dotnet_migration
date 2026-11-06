# Shared names for every stage. No secrets in here.
# Usage: source infra/env.sh
# Change SUFFIX once to something unique to you (lowercase letters and digits only),
# because SQL server, web app and Key Vault names must be unique across all of Azure.
export SUFFIX="jacreator2"
export SQL_ADMIN_PASSWORD="LocalDev_Only_123!"

export RG="rg-readings-demo"
# Azure for Students only allows: spaincentral, norwayeast, italynorth, denmarkeast, switzerlandnorth
export LOC="spaincentral"   # switzerlandnorth and norwayeast refuse new SQL servers (capacity)
export SQL_SERVER="sql-readings-$SUFFIX"
export SQL_DB="ReadingsDb"
export SQL_ADMIN_USER="readingsadmin"
export PLAN="plan-readings-demo"
export LEGACY_APP="app-readings-legacy-$SUFFIX"
export LOG_WS="log-readings-demo"
export APPI="appi-readings-demo"
export KV="kv-readings-$SUFFIX"
export WEB_APP="app-readings-web-$SUFFIX"
