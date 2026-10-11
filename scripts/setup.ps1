# Interactive first-time setup (Windows PowerShell). Mirrors scripts/setup.sh.

$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location $Root

function Ask([string]$var, [string]$prompt, [string]$default = '') {
    $answer = if ($default) {
        Read-Host "$prompt [$default]"
        if ([string]::IsNullOrWhiteSpace($_)) { $default } else { $_ }
    } else {
        Read-Host "$prompt"
    }
    Set-Variable -Name $var -Value $answer -Scope Script
}

Write-Host '=== Verificando prerrequisitos ===' -ForegroundColor Cyan
& "$Root/scripts/doctor.ps1"
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host '=== Perfil ===' -ForegroundColor Cyan
Ask PROFILE 'Perfil (simple|erp|saas|saas-dedicated)' 'simple'

Write-Host '=== Motor de BD ===' -ForegroundColor Cyan
Ask PROVIDER 'Proveedor (SqlServer|PostgreSql|MySql)' 'PostgreSql'
Ask DB_CONNECTION 'Connection string' 'Host=localhost;Database=plantilla;Username=plantilla;Password=plantilla'

Write-Host '=== Secretos ===' -ForegroundColor Cyan
Ask JWT_KEY 'Jwt:SigningKey (>= 32 bytes, vacio para generar)' ''
if ([string]::IsNullOrWhiteSpace($JWT_KEY)) {
    $bytes = New-Object byte[] 48
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    $JWT_KEY = [Convert]::ToBase64String($bytes)
    Write-Host "Generado: $JWT_KEY"
}
Ask SEED_PASSWORD 'Seed:AdminPassword' '1234567'

Write-Host '=== Organización ===' -ForegroundColor Cyan
Ask ORG 'Activar OrganizationModule? (y/n)' 'n'

$tenancyMode = switch ($PROFILE) {
    'saas-dedicated' { 'Hybrid' }
    'saas' { 'SharedDatabase' }
    default { 'None' }
}
$disabledModules = if ($ORG -eq 'y') { '[]' } else { '["Organization"]' }

$json = @{
    Tenancy  = @{ Mode = $tenancyMode }
    Database = @{
        Provider        = $PROVIDER
        ConnectionStrings = @{ Default = $DB_CONNECTION }
    }
    Modules  = @{ Disabled = $disabledModules | ConvertFrom-Json }
} | ConvertTo-Json -Depth 4

$path = 'src/Host/Project.Api/appsettings.Development.json'
Set-Content -Path $path -Value $json -Encoding UTF8

dotnet user-secrets init --project src/Host/Project.Api *> $null 2>&1
dotnet user-secrets set "Jwt:SigningKey" "$JWT_KEY" --project src/Host/Project.Api *> $null
dotnet user-secrets set "Seed:AdminPassword" "$SEED_PASSWORD" --project src/Host/Project.Api *> $null

Write-Host ''
Write-Host 'Configuracion escrita. Proximos pasos:' -ForegroundColor Green
Write-Host '  1. cd src/Host/Project.MigrationService && dotnet run -- migrate'
Write-Host '  2. dotnet run --project src/Host/Project.Api'
Write-Host '  3. cd project.client && pnpm dev'