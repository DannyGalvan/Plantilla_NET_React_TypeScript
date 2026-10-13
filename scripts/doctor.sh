#!/usr/bin/env bash
# Checks the local prerequisites. Prints OK, FALTA (missing, blocking) or ADVERTENCIA (optional).
# macOS / Linux. Exit code 1 when something blocking is missing.

cd "$(dirname "$0")/.." || exit 1
MISSING=0

ok() { printf "  OK           %s\n" "$1"; }
missing() { printf "  FALTA        %s\n" "$1"; MISSING=1; }
warn() { printf "  ADVERTENCIA  %s\n" "$1"; }

if command -v dotnet >/dev/null 2>&1; then
  sdk=$(dotnet --version 2>/dev/null)
  [[ "$sdk" == 10.* ]] && ok ".NET SDK $sdk" || missing ".NET SDK 10 is required (found $sdk)"
else
  missing ".NET SDK 10 (https://dotnet.microsoft.com/download)"
fi

if command -v node >/dev/null 2>&1; then
  node_version=$(node --version | tr -d v)
  major=${node_version%%.*}
  (( major >= 24 )) && ok "Node $node_version" || missing "Node 24 or newer is required (found $node_version)"
else
  missing "Node 24 (https://nodejs.org or fnm/nvm)"
fi

if command -v pnpm >/dev/null 2>&1; then
  ok "pnpm $(pnpm --version)"
else
  missing "pnpm (run: corepack enable)"
fi

if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  ok "Docker $(docker --version | awk '{print $3}' | tr -d ,)"
else
  warn "Docker is not running (needed for docker compose and the functional tests)"
fi

if dotnet dev-certs https --check >/dev/null 2>&1; then
  ok "ASP.NET Core HTTPS development certificate"
else
  warn "HTTPS development certificate missing (run: dotnet dev-certs https --trust)"
fi

if [[ $MISSING -eq 1 ]]; then
  echo "Result: FALTA. Install the missing tools and run the check again."
  exit 1
fi

echo "Result: OK."
