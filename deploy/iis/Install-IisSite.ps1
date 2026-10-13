# Creates (or updates) the IIS application pool and site for the API. Run as Administrator on the server.
# Requires: IIS with the Web Server role and the ASP.NET Core 10 Hosting Bundle (restart IIS after installing it).
#
# Example:
#   ./Install-IisSite.ps1 -SitePath C:\sites\project -HostName erp.example.com -CertificateThumbprint <thumbprint>

param(
    [Parameter(Mandatory = $true)][string] $SitePath,
    [Parameter(Mandatory = $true)][string] $HostName,
    [string] $SiteName = 'Project',
    [string] $CertificateThumbprint
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

if (-not (Test-Path "IIS:\AppPools\$SiteName")) {
    New-WebAppPool -Name $SiteName | Out-Null
}

# No managed code (ASP.NET Core runs its own runtime), always running so background services (outbox) keep working.
Set-ItemProperty "IIS:\AppPools\$SiteName" -Name managedRuntimeVersion -Value ''
Set-ItemProperty "IIS:\AppPools\$SiteName" -Name startMode -Value 'AlwaysRunning'
Set-ItemProperty "IIS:\AppPools\$SiteName" -Name processModel.idleTimeout -Value '00:00:00'
Set-ItemProperty "IIS:\AppPools\$SiteName" -Name processModel.identityType -Value 'ApplicationPoolIdentity'

New-Item -ItemType Directory -Force -Path $SitePath, (Join-Path $SitePath 'logs') | Out-Null

if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName -PhysicalPath $SitePath -ApplicationPool $SiteName -HostHeader $HostName -Port 80 | Out-Null
}

Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationDefaults.preloadEnabled -Value $true

if ($CertificateThumbprint) {
    if (-not (Get-WebBinding -Name $SiteName -Protocol https)) {
        New-WebBinding -Name $SiteName -Protocol https -Port 443 -HostHeader $HostName -SslFlags 1
    }

    (Get-WebBinding -Name $SiteName -Protocol https).AddSslCertificate($CertificateThumbprint, 'my')
}

$identity = "IIS AppPool\$SiteName"
icacls $SitePath /grant "${identity}:(OI)(CI)RX" | Out-Null
icacls (Join-Path $SitePath 'logs') /grant "${identity}:(OI)(CI)M" | Out-Null

Write-Host "OK: site '$SiteName' ready at $SitePath. Configure secrets as environment variables of the app pool:"
Write-Host "  appcmd set config -section:system.applicationHost/applicationPools /+`"[name='$SiteName'].environmentVariables.[name='Configuration__MasterKey',value='...']`" /commit:apphost"
