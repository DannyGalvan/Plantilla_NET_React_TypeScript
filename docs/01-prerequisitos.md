# 01 · Prerrequisitos

## Lista de herramientas

| Herramienta | Versión | Para qué | Obligatoria |
|---|---|---|---|
| .NET SDK | 10.0.x (de `global.json`) | backend | Sí (salvo camino solo-Docker) |
| Node.js | 24.15.x (de `.nvmrc`) | frontend | Sí (salvo camino solo-Docker) |
| pnpm vía Corepack | de `packageManager` | frontend | Sí |
| Docker Desktop / Engine + Compose v2 | — | contenedores (BD, Redis, Aspire) | Recomendada |
| dotnet-ef (tool local `.config/dotnet-tools.json`) | 10.x | migraciones | Sí para desarrollo |
| Git | ≥ 2.40 | — | Sí |
| Motor de BD local | SQL Server 2019+, PostgreSQL 15+ o MySQL 8+ | si no usas Docker | Opcional |
| IDE | Rider, VS 2022 17.14+ / VS 2026, o VS Code + C# Dev Kit | — | Recomendada |

> Las versiones exactas se centralizan en `global.json` (SDK .NET) y
> `.nvmrc` (Node). El script `scripts/doctor` valida que tu entorno coincida
> con esas versiones antes de empezar.

## Instalación por sistema operativo

### Windows

```powershell
winget install Microsoft.DotNet.SDK.10
winget install OpenJS.NodeJS.LTS
corepack enable
winget install Docker.DockerDesktop
dotnet dev-certs https --trust
dotnet tool restore   # restaura dotnet-ef desde .config/dotnet-tools.json
```

### macOS

```bash
brew install --cask dotnet-sdk
brew install node@24
corepack enable
brew install --cask docker
dotnet dev-certs https --trust
dotnet tool restore
```

### Linux (Ubuntu / Debian)

```bash
# .NET 10: usar el script de Microsoft o instalar via snap
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
sudo /tmp/dotnet-install.sh --channel 10.0

# Node 24 via NodeSource
curl -fsSL https://deb.nodesource.com/setup_24.x | sudo -E bash -
sudo apt install -y nodejs
corepack enable

# Docker Engine + Compose
sudo apt install -y docker.io docker-compose-plugin
sudo usermod -aG docker $USER
dotnet dev-certs https --trust
dotnet tool restore
```

### Linux (RHEL / Fedora)

```bash
sudo dnf install -y dotnet-sdk-10.0 nodejs docker docker-compose
sudo systemctl enable --now docker
corepack enable
dotnet dev-certs https --trust
dotnet tool restore
```

## Verificación

```bash
scripts/doctor   # en macOS / Linux
scripts/doctor.ps1   # en Windows
```

`doctor` imprime `OK`, `FALTA` o `ADVERTENCIA` por cada requisito. Si
reporta `FALTA` en .NET SDK, no sigas: la versión que tienes no compila el
repositorio.