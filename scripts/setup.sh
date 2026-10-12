#!/usr/bin/env bash
# First-time local setup (macOS / Linux).
# Stores the local configuration in .NET user-secrets (never in the repository), restores tools and packages.
#
# Usage:
#   ./scripts/setup.sh                       interactive
#   ./scripts/setup.sh --non-interactive     defaults (PostgreSql from docker-compose, generated secrets)

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

INTERACTIVE=true
[[ "${1:-}" == "--non-interactive" || "${CI:-}" == "true" ]] && INTERACTIVE=false

ask() {
  local prompt="$1" default="$2" answer
  if $INTERACTIVE; then
    read -r -p "$prompt [$default]: " answer
    echo "${answer:-$default}"
  else
    echo "$default"
  fi
}

random() { openssl rand -base64 48 | tr -d '\n/+=' | cut -c1-"$1"; }

echo "== Prerequisites"
bash scripts/doctor.sh || { echo "Install the missing tools (FALTA) and run setup again."; exit 1; }

echo "== Configuration"
PROVIDER=$(ask "Database provider (PostgreSql|SqlServer|MySql)" "PostgreSql")
case "$PROVIDER" in
  SqlServer) DEFAULT_CS="Server=localhost,1433;Database=plantilla;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True" ;;
  MySql) DEFAULT_CS="Server=localhost;Port=3306;Database=plantilla;User=root;Password=root" ;;
  *) DEFAULT_CS="Host=localhost;Database=plantilla;Username=plantilla;Password=plantilla" ;;
esac
CONNECTION=$(ask "Connection string" "$DEFAULT_CS")
MODE=$(ask "Tenancy mode (None|SharedDatabase|DatabasePerTenant|Hybrid)" "None")
ADMIN_PASSWORD="Adm-$(random 12)!a1"

PROJECT=src/Host/Project.Api
dotnet user-secrets set "Database:Provider" "$PROVIDER" --project "$PROJECT" >/dev/null
dotnet user-secrets set "Database:ConnectionStrings:Default" "$CONNECTION" --project "$PROJECT" >/dev/null
dotnet user-secrets set "Tenancy:Mode" "$MODE" --project "$PROJECT" >/dev/null
dotnet user-secrets set "Jwt:SigningKey" "$(random 64)" --project "$PROJECT" >/dev/null
dotnet user-secrets set "Seed:AdminPassword" "$ADMIN_PASSWORD" --project "$PROJECT" >/dev/null
echo "OK user-secrets written (shared by Project.Api and Project.MigrationService)."

echo "== Tools and packages"
dotnet tool restore >/dev/null
dotnet restore Project.slnx >/dev/null
(cd src/project.client && corepack enable >/dev/null 2>&1 || true; pnpm install --frozen-lockfile >/dev/null)
echo "OK dependencies restored."

cat <<EOF

Setup finished.
  First administrator: admin / $ADMIN_PASSWORD   (you must change it at the first sign-in)

Next steps:
  docker compose --profile postgres up -d        (skip if you use your own database)
  dotnet run --project src/Host/Project.Api      (migrates and seeds in Development, starts the SPA with Vite)
EOF
