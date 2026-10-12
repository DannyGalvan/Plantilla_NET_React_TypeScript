# Requisitos previos

**Para qué sirve este documento:** instalar las herramientas necesarias en Windows, macOS o Linux.
**Tiempo estimado:** 15 a 30 minutos.

## Herramientas

| Herramienta | Versión | Para qué | Obligatoria |
|---|---|---|---|
| .NET SDK | 10.0 (ver `global.json`) | Backend | Sí, salvo si solo usas Docker |
| Node.js | 24 (ver `src/project.client/.nvmrc`) | Frontend | Sí, salvo si solo usas Docker |
| pnpm | 9.15 o superior (vía Corepack) | Paquetes del frontend | Sí, salvo si solo usas Docker |
| Docker con Compose v2 | Reciente | Bases de datos locales y pruebas funcionales | Recomendada (obligatoria para `dotnet test`) |
| Git | 2.40 o superior | Control de versiones | Sí |
| Motor de base de datos | PostgreSQL 15+, SQL Server 2019+ o MySQL 8+ | Si no usas Docker | Opcional |
| IDE | Rider, Visual Studio 2022 17.14+ o VS Code con C# Dev Kit | Desarrollo | Recomendada |

La herramienta `dotnet-ef` se instala como herramienta local del repositorio con `dotnet tool restore`.

## Windows

```powershell
winget install Microsoft.DotNet.SDK.10
winget install OpenJS.NodeJS.LTS
winget install Docker.DockerDesktop      # requiere WSL2
corepack enable
dotnet dev-certs https --trust
```

## macOS

```bash
brew install --cask dotnet-sdk
brew install node@24
brew install --cask docker               # u OrbStack
corepack enable
dotnet dev-certs https --trust
```

## Linux (Ubuntu / Debian)

```bash
sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
curl -fsSL https://fnm.vercel.app/install | bash && fnm install 24
corepack enable
# Docker Engine: https://docs.docker.com/engine/install/ubuntu/
dotnet dev-certs https                    # en Linux confía el certificado manualmente (ver troubleshooting)
```

Para RHEL o Fedora usa `sudo dnf install dotnet-sdk-10.0`.

## Verificar

```bash
./scripts/doctor.sh
```

```powershell
./scripts/doctor.ps1
```

Cada requisito se reporta como `OK`, `FALTA` (bloquea) o `ADVERTENCIA` (opcional).
