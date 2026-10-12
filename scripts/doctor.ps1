# Checks the local prerequisites (Windows / PowerShell 7). Mirrors scripts/doctor.sh.
# Prints OK, FALTA (missing, blocking) or ADVERTENCIA (optional). Exit code 1 when something blocking is missing.

Set-Location (Join-Path $PSScriptRoot '..')
$script:Missing = $false

function Ok([string] $Text) { Write-Host "  OK           $Text" }
function Missing([string] $Text) { Write-Host "  FALTA        $Text"; $script:Missing = $true }
function Warn([string] $Text) { Write-Host "  ADVERTENCIA  $Text" }

if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $sdk = (dotnet --version).Trim()
    if ($sdk -like '10.*') { Ok ".NET SDK $sdk" } else { Missing ".NET SDK 10 is required (found $sdk)" }
} else { Missing '.NET SDK 10 (winget install Microsoft.DotNet.SDK.10)' }

if (Get-Command node -ErrorAction SilentlyContinue) {
    $nodeVersion = (node --version).Trim().TrimStart('v')
    if ([int]($nodeVersion.Split('.')[0]) -ge 24) { Ok "Node $nodeVersion" } else { Missing "Node 24 or newer is required (found $nodeVersion)" }
} else { Missing 'Node 24 (winget install OpenJS.NodeJS.LTS)' }

if (Get-Command pnpm -ErrorAction SilentlyContinue) { Ok "pnpm $((pnpm --version).Trim())" } else { Missing 'pnpm (run: corepack enable)' }

if ((Get-Command docker -ErrorAction SilentlyContinue) -and (docker info 2>$null)) { Ok 'Docker' }
else { Warn 'Docker is not running (needed for docker compose and the functional tests)' }

dotnet dev-certs https --check *> $null
if ($LASTEXITCODE -eq 0) { Ok 'ASP.NET Core HTTPS development certificate' }
else { Warn 'HTTPS development certificate missing (run: dotnet dev-certs https --trust)' }

if ($script:Missing) {
    Write-Host 'Result: FALTA. Install the missing tools and run the check again.'
    exit 1
}

Write-Host 'Result: OK.'
exit 0
