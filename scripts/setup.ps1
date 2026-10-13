# First-time local setup (Windows / PowerShell 7). Mirrors scripts/setup.sh.
# Stores the local configuration in .NET user-secrets (never in the repository), restores tools and packages.
#
# Usage:
#   ./scripts/setup.ps1                     interactive
#   ./scripts/setup.ps1 -NonInteractive     defaults (PostgreSql from docker-compose, generated secrets)

param([switch] $NonInteractive)

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')
if ($env:CI -eq 'true') { $NonInteractive = $true }

function Ask([string] $Prompt, [string] $Default) {
    if ($NonInteractive) { return $Default }
    $answer = Read-Host "$Prompt [$Default]"
    if ([string]::IsNullOrWhiteSpace($answer)) { return $Default } else { return $answer }
}

function New-Secret([int] $Length) {
    $bytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64)
    return ([Convert]::ToBase64String($bytes) -replace '[/+=]', '').Substring(0, $Length)
}

Write-Host '== Prerequisites'
& (Join-Path $PSScriptRoot 'doctor.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Install the missing tools (FALTA) and run setup again.' }

Write-Host '== Configuration'
$provider = Ask 'Database provider (PostgreSql|SqlServer|MySql)' 'PostgreSql'
$defaultConnection = switch ($provider) {
    'SqlServer' { 'Server=localhost,1433;Database=plantilla;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True' }
    'MySql' { 'Server=localhost;Port=3306;Database=plantilla;User=root;Password=root' }
    default { 'Host=localhost;Database=plantilla;Username=plantilla;Password=plantilla' }
}
$connection = Ask 'Connection string' $defaultConnection
$mode = Ask 'Tenancy mode (None|SharedDatabase|DatabasePerTenant|Hybrid)' 'None'
$adminPassword = "Adm-$(New-Secret 12)!a1"

$project = 'src/Host/Project.Api'
dotnet user-secrets set 'Database:Provider' $provider --project $project | Out-Null
dotnet user-secrets set 'Database:ConnectionStrings:Default' $connection --project $project | Out-Null
dotnet user-secrets set 'Tenancy:Mode' $mode --project $project | Out-Null
dotnet user-secrets set 'Jwt:SigningKey' (New-Secret 64) --project $project | Out-Null
dotnet user-secrets set 'Seed:AdminPassword' $adminPassword --project $project | Out-Null
Write-Host 'OK user-secrets written (shared by Project.Api and Project.MigrationService).'

Write-Host '== Tools and packages'
dotnet tool restore | Out-Null
dotnet restore Project.slnx | Out-Null
Push-Location src/project.client
try { corepack enable 2>$null } catch { }
pnpm install --frozen-lockfile | Out-Null
Pop-Location
Write-Host 'OK dependencies restored.'

Write-Host ''
Write-Host 'Setup finished.'
Write-Host "  First administrator: admin / $adminPassword   (you must change it at the first sign-in)"
Write-Host ''
Write-Host 'Next steps:'
Write-Host '  docker compose --profile postgres up -d        (skip if you use your own database)'
Write-Host '  dotnet run --project src/Host/Project.Api      (migrates and seeds in Development, starts the SPA with Vite)'
