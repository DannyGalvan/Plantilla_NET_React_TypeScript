#!/usr/bin/env bash
# Generates an EF Core migration for the given module across all three
# providers (SqlServer, PostgreSql, MySql). Requires the .NET SDK and the
# dotnet-ef global tool installed via `dotnet tool restore` from .config/dotnet-tools.json.
#
# Usage: ./scripts/add-migration.sh Catalog InitialCreate

set -euo pipefail

MODULE="${1:?module name required}"
NAME="${2:?migration name required}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

cd "$ROOT"

for provider in SqlServer PostgreSql MySql; do
  project="src/Migrations/Project.Migrations.$provider/Project.Migrations.$provider.csproj"
  if [[ ! -f "$project" ]]; then
    echo "Skipping $provider ($project not found)"
    continue
  fi

  echo "==> $provider"
  dotnet ef migrations add "$NAME" \
    --project "$project" \
    --startup-project "$project" \
    --output-dir "$MODULE"
done