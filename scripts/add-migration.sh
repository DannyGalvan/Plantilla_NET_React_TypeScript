#!/usr/bin/env bash
# Generates an EF Core migration for one module in every database engine (SqlServer, PostgreSql, MySql).
# Requires the .NET SDK; the dotnet-ef tool is restored from dotnet-tools.json.
#
# Usage: ./scripts/add-migration.sh <Module> <MigrationName>
# Example: ./scripts/add-migration.sh Catalog AddProductBarcode

set -euo pipefail

MODULE="${1:?Usage: add-migration.sh <Module> <MigrationName>}"
NAME="${2:?Usage: add-migration.sh <Module> <MigrationName>}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

dotnet tool restore >/dev/null

for provider in SqlServer PostgreSql MySql; do
  project="src/Migrations/Project.Migrations.$provider/Project.Migrations.$provider.csproj"
  echo "==> $MODULE / $provider"
  dotnet ef migrations add "$NAME" \
    --project "$project" \
    --startup-project "$project" \
    --context "${MODULE}DbContext" \
    --output-dir "$MODULE"
done

echo "OK: migration '$NAME' created for module '$MODULE' in the three engines."
