# Diagnostic script (Windows). Reports OK, FALTA or ADVERTENCIA per
# requirement. Mirrors scripts/doctor.sh.

$ErrorActionPreference = 'Stop'

$Global:Falta = $false

function Ok($title)    { Write-Host "  OK         $title" -ForegroundColor Green }
function Falta($title) { Write-Host "  FALTA      $title" -ForegroundColor Red;   $Global:Falta = $true }
function Adv($title)   { Write-Host "  ADVERTENCIA $title" -ForegroundColor Yellow }

Write-Host "=== .NET SDK ==="
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $version = (& dotnet --version) 2>$null
    $expected = (Get-Content global.json | Select-String -Pattern '"[0-9]+\.[0-9]+\.[0-9]+' | Select-Object -First 1).Matches.Value
    if ($version -like "$expected*") { Ok ".NET SDK $version" } else { Adv ".NET SDK $version (global.json pide $expected)" }
} else { Falta ".NET SDK no encontrado" }

Write-Host "=== Node ==="
if (Get-Command node -ErrorAction SilentlyContinue) {
    $version = (& node --version) 2>$null
    $expected = (Get-Content .nvmrc).Trim().TrimStart('v')
    if ($version -eq "v$expected") { Ok "Node $version" } else { Adv "Node $version (.nvmrc pide $expected)" }
} else { Falta "Node no encontrado" }

Write-Host "=== pnpm ==="
if (Get-Command pnpm -ErrorAction SilentlyContinue) {
    Ok "pnpm $((pnpm --version) 2>$null)"
} else { Falta "pnpm no encontrado (corepack enable)" }

Write-Host "=== Docker ==="
if (Get-Command docker -ErrorAction SilentlyContinue) {
    if ($null -ne (docker info 2>$null)) { Ok "Docker $((docker --version) 2>$null | Select-Object -First 1)" }
    else { Adv "Docker instalado pero el daemon no responde" }
} else { Adv "Docker no encontrado (recomendado)" }

Write-Host "=== dotnet-ef ==="
if ($null -ne (dotnet ef --version 2>$null)) { Ok "dotnet-ef $(dotnet ef --version 2>$null | Select-Object -First 1)" }
else { Adv "dotnet-ef no instalado (dotnet tool restore)" }

Write-Host "=== Certificados de desarrollo ==="
$cert = (& dotnet dev-certs https --check) 2>$null
if ($LASTEXITCODE -eq 0) { Ok "Certificado HTTPS de desarrollo válido" }
else { Adv "Certificado HTTPS ausente (dotnet dev-certs https --trust)" }

Write-Host "=== Variables de entorno ==="
if ($env:Jwt__SigningKey) { Ok "Jwt__SigningKey presente" } else { Adv "Jwt__SigningKey no definido (user-secrets o env var)" }
if ($env:ConnectionStrings__Default) { Ok "ConnectionStrings__Default presente" }
else { Falta "ConnectionStrings__Default no definido" }

if ($Global:Falta) {
    Write-Host ""
    Write-Host "Resultado: FALTA. Corrige los puntos marcados antes de continuar."
    exit 1
}
Write-Host ""
Write-Host "Resultado: requisitos mínimos OK."
exit 0