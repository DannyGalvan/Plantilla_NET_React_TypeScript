# Generates an EF Core migration for one module in every database engine (SqlServer, PostgreSql, MySql).
# Requires the .NET SDK; the dotnet-ef tool is restored from dotnet-tools.json.
#
# Usage: ./scripts/add-migration.ps1 -Module <Module> -Name <MigrationName>
# Example: ./scripts/add-migration.ps1 -Module Catalog -Name AddProductBarcode

param(
    [Parameter(Mandatory = $true)][string] $Module,
    [Parameter(Mandatory = $true)][string] $Name
)

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

dotnet tool restore | Out-Null

foreach ($provider in @('SqlServer', 'PostgreSql', 'MySql')) {
    $project = "src/Migrations/Project.Migrations.$provider/Project.Migrations.$provider.csproj"
    Write-Host "==> $Module / $provider"
    dotnet ef migrations add $Name --project $project --startup-project $project --context "$($Module)DbContext" --output-dir $Module
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef failed for $provider" }
}

Write-Host "OK: migration '$Name' created for module '$Module' in the three engines."
