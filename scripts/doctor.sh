#!/usr/bin/env bash
# Diagnostic script. Reports OK, FALTA or ADVERTENCIA per requirement.
# Used locally and by CI (scripts/quickstart-matrix). macOS/Linux.

ok() { printf "  \033[32mOK\033[0m  %s\n" "$1"; }
falta() { printf "  \033[31mFALTA\033[0m  %s\n" "$1"; FALTA=1; }
adv() { printf "  \033[33mADVERTENCIA\033[0m  %s\n" "$1"; }

FALTA=0

echo "=== .NET SDK ==="
if command -v dotnet >/dev/null 2>&1; then
  version=$(dotnet --version 2>/dev/null)
  expected=$(grep -oE '"[0-9]+\.[0-9]+\.[0-9]+(-[a-z0-9.]+)?' global.json | tr -d 'v' | head -n1)
  if [[ "$version" == "$expected"* ]]; then
    ok ".NET SDK $version"
  else
    adv ".NET SDK $version (global.json pide $expected)"
  fi
else
  falta ".NET SDK no encontrado"
fi

echo "=== Node ==="
if command -v node >/dev/null 2>&1; then
  version=$(node --version 2>/dev/null)
  expected=$(cat .nvmrc 2>/dev/null | tr -d 'v')
  if [[ "$version" == "v$expected" ]]; then
    ok "Node $version"
  else
    adv "Node $version (.nvmrc pide $expected)"
  fi
else
  falta "Node no encontrado"
fi

echo "=== pnpm ==="
if command -v pnpm >/dev/null 2>&1; then
  ok "pnpm $(pnpm --version)"
else
  falta "pnpm no encontrado (corepack enable)"
fi

echo "=== Docker ==="
if command -v docker >/dev/null 2>&1; then
  if docker info >/dev/null 2>&1; then
    ok "Docker $(docker --version | head -n1)"
  else
    adv "Docker instalado pero el daemon no responde"
  fi
else
  adv "Docker no encontrado (recomendado)"
fi

echo "=== dotnet-ef ==="
if dotnet ef --version >/dev/null 2>&1; then
  ok "dotnet-ef $(dotnet ef --version | head -n1)"
else
  adv "dotnet-ef no instalado (dotnet tool restore)"
fi

echo "=== Certificados de desarrollo ==="
if dotnet dev-certs https --check >/dev/null 2>&1; then
  ok "Certificado HTTPS de desarrollo válido"
else
  adv "Certificado HTTPS ausente (dotnet dev-certs https --trust)"
fi

echo "=== Variables de entorno ==="
if [[ -n "$Jwt__SigningKey" ]]; then
  ok "Jwt__SigningKey presente"
else
  adv "Jwt__SigningKey no definido (user-secrets o env var)"
fi

if [[ -n "$ConnectionStrings__Default" ]]; then
  ok "ConnectionStrings__Default presente"
else
  falta "ConnectionStrings__Default no definido"
fi

if [[ $FALTA -eq 1 ]]; then
  echo
  echo "Resultado: FALTA. Corrige los puntos marcados antes de continuar."
  exit 1
fi
echo
echo "Resultado: requisitos mínimos OK."
exit 0