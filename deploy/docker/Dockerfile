# API + SPA in one image. Build from the repository root:
#   docker build -f deploy/docker/Dockerfile -t project-api .

# ---- 1. Frontend ----
FROM node:24-alpine AS spa
WORKDIR /src/project.client
RUN corepack enable
COPY src/project.client/package.json src/project.client/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY src/project.client/ ./
RUN pnpm build

# ---- 2. Backend ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet restore src/Host/Project.Api/Project.Api.csproj
RUN dotnet publish src/Host/Project.Api/Project.Api.csproj -c Release -o /app --no-restore -p:SkipSpaBuild=true
COPY --from=spa /src/project.client/dist /app/wwwroot

# ---- 3. Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app .
USER app
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD ["dotnet", "Project.Api.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "Project.Api.dll"]
