# Despliegue en Linux con systemd y nginx

**Para qué sirve este documento:** publicar la API como servicio systemd detrás de nginx.
**Requisitos previos:** Ubuntu 22.04/24.04 o RHEL 9, runtime ASP.NET Core 10, nginx. **Tiempo estimado:** 30 minutos.

## 1. Publicar (en la máquina de build)

```bash
dotnet publish src/Host/Project.Api -c Release -o out/api                 # incluye la SPA (requiere Node y pnpm)
dotnet publish src/Host/Project.MigrationService -c Release -o out/migrations
```

Para no instalar el runtime en el servidor agrega `-r linux-x64 --self-contained true` y cambia `ExecStart` al ejecutable.

## 2. Instalar (en el servidor)

```bash
sudo apt-get install -y aspnetcore-runtime-10.0 nginx rsync      # RHEL: sudo dnf install aspnetcore-runtime-10.0 nginx rsync
sudo ./deploy/linux/install.sh out
sudo nano /etc/project/api.env                                  # completa los valores <...>
```

`install.sh` crea el usuario de sistema `project`, copia los binarios a `/opt/project`, instala las unidades
`project-api.service` y `project-migrations.service` y crea `/etc/project/api.env` (permisos 600) a partir de
`deploy/linux/api.env.example`.

## 3. Migrar e iniciar

```bash
sudo systemctl start project-migrations     # oneshot: migra, sincroniza permisos y siembra
sudo systemctl enable --now project-api     # Kestrel en http://127.0.0.1:5000
```

## 4. nginx y TLS

```bash
sudo cp deploy/linux/nginx-project.conf /etc/nginx/conf.d/project.conf   # cambia erp.example.com y los certificados
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d erp.example.com
```

`api.env` ya declara `Hosting__KnownProxies__0=127.0.0.1` para confiar en las cabeceras `X-Forwarded-*` de nginx.

## Cómo verificar que funcionó

```bash
systemctl status project-api
curl -s http://127.0.0.1:5000/health/ready
journalctl -u project-api -f
```

## Actualizar

```bash
sudo ./deploy/linux/install.sh out
sudo systemctl start project-migrations && sudo systemctl restart project-api
```

Sin caída: dos instancias en puertos distintos (copia la unidad con otro `ASPNETCORE_URLS`) en el `upstream` de nginx y
reinícialas una a la vez. Requiere Redis.

## Problemas comunes

Ver [troubleshooting.md](../troubleshooting.md#linux).
