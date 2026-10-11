#!/usr/bin/env bash
# Interactive first-time setup. Verifies prerequisites via scripts/doctor,
# then walks the operator through the day-1 checklist from
# docs/03-configuracion-inicial.md.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

bold() { printf "\033[1m%s\033[0m\n" "$*"; }
ask() {
  local var="$1" prompt="$2" default="${3:-}"
  local answer
  if [[ -n "$default" ]]; then
    read -r -p "$prompt [$default]: " answer
    answer="${answer:-$default}"
  else
    read -r -p "$prompt: " answer
  fi
  eval "$var=\"\$answer\""
}

bold "=== Verificando prerrequisitos ==="
if ! bash scripts/doctor.sh; then
  echo "Corrige los puntos FALTA antes de continuar."
  exit 1
fi

bold "=== Perfil ==="
ask PROFILE "Perfil (simple|erp|saas|saas-dedicated)" "simple"

bold "=== Motor de BD ==="
ask PROVIDER "Proveedor (SqlServer|PostgreSql|MySql)" "PostgreSql"
ask DB_CONNECTION "Connection string" "Host=localhost;Database=plantilla;Username=plantilla;Password=plantilla"

bold "=== Secretos ==="
ask JWT_KEY "Jwt:SigningKey (≥ 32 bytes, deja vacío para generar)" ""
if [[ -z "$JWT_KEY" ]]; then
  JWT_KEY=$(openssl rand -base64 48)
  echo "Generado: $JWT_KEY"
fi
ask SEED_PASSWORD "Seed:AdminPassword" "1234567"

bold "=== Organización ==="
ask ORG "Activar OrganizationModule? (y/n)" "n"

bold "=== Resumen ==="
cat <<EOF
Perfil:                $PROFILE
Proveedor:             $PROVIDER
Connection string:     $DB_CONNECTION
Jwt:SigningKey:        ${JWT_KEY:0:8}...
Seed:AdminPassword:    $SEED_PASSWORD
Organization:          $ORG
EOF

cat > src/Host/Project.Api/appsettings.Development.json <<JSON
{
  "Tenancy": { "Mode": "$([[ "$PROFILE" == saas-dedicated ]] && echo Hybrid || ([[ "$PROFILE" == saas ]] && echo SharedDatabase || echo None))" },
  "Database": {
    "Provider": "$PROVIDER",
    "ConnectionStrings": { "Default": "$DB_CONNECTION" }
  },
  "Modules": { "Disabled": $([[ "$ORG" == "y" ]] && echo '[]' || echo '["Organization"]') }
}
JSON

# User-secrets para Development
dotnet user-secrets init --project src/Host/Project.Api >/dev/null 2>&1 || true
dotnet user-secrets set "Jwt:SigningKey" "$JWT_KEY" --project src/Host/Project.Api
dotnet user-secrets set "Seed:AdminPassword" "$SEED_PASSWORD" --project src/Host/Project.Api

echo
echo "Configuración escrita. Próximos pasos:"
echo "  1. cd src/Host/Project.MigrationService && dotnet run -- migrate"
echo "  2. dotnet run --project src/Host/Project.Api"
echo "  3. cd project.client && pnpm dev"