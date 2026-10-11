# Despliegue en Linux + systemd + nginx

> Ubuntu 22.04/24.04 o RHEL 9 con el runtime de ASP.NET Core 10. La SPA la
> sirve nginx directamente; el API corre detrás de un socket TCP interno.

## Requisitos

```bash
# Ubuntu / Debian
sudo apt install -y aspnetcore-runtime-10.0 nginx

# RHEL / Fedora
sudo dnf install -y aspnetcore-runtime-10.0 nginx
```

## Publicar

```bash
dotnet publish src/Host/Project.Api/Project.Api.csproj \
  -c Release \
  -o ./out
```

## Estructura en el servidor

```
/opt/project/
  api/
    Project.Api.dll
    appsettings.json
  logs/
/etc/project/
  api.env                 # EnvironmentFile= con los secretos
/etc/systemd/system/
  project-api.service
/etc/nginx/sites-available/
  project.conf
```

## `project-api.service`

```ini
[Unit]
Description=Plantilla API
After=network.target
Wants=network-online.target

[Service]
Type=simple
User=project
Group=project
WorkingDirectory=/opt/project/api
ExecStart=/usr/bin/dotnet /opt/project/api/Project.Api.dll
Restart=always
RestartSec=5

# Hardening
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/project/logs
PrivateTmp=true
PrivateDevices=true

# Recursos
MemoryMax=512M
TasksMax=512

# Secretos vía EnvironmentFile (chmod 600)
EnvironmentFile=/etc/project/api.env

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now project-api
sudo journalctl -u project-api -f
```

## `/etc/nginx/sites-available/project.conf`

```nginx
upstream project_api {
    server 127.0.0.1:5000;
    keepalive 32;
}

server {
    listen 80;
    listen [::]:80;
    server_name app.example.com;

    # Redirect HTTP -> HTTPS en producción
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name app.example.com;

    ssl_certificate /etc/letsencrypt/live/app.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/app.example.com/privkey.pem;
    include /etc/letsencrypt/options-ssl-nginx.conf;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "no-referrer" always;
    add_header Permissions-Policy "geolocation=(), microphone=(), camera=()" always;

    client_max_body_size 25m;

    # gzip / brotli
    gzip on;
    gzip_types text/plain text/css application/json application/javascript;

    # Frontend estático
    root /opt/project/api/wwwroot;
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API
    location /api/ {
        proxy_pass http://project_api;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Connection "";
        proxy_read_timeout 60s;
    }

    # Forwarded headers en el API (recordar KnownProxies/KnownNetworks)
    # Los añade Program.cs cuando el header X-Forwarded-For llega.

    # Health
    location /health/ { proxy_pass http://project_api; }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/project.conf /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## TLS con Let's Encrypt

```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d app.example.com
```

El certificado se renueva automáticamente vía systemd timer.

## Actualización sin caída

Dos instancias en puertos distintos, balanceadas por nginx con
`least_conn` o `ip_hash`. Subes la nueva al puerto 5001, validas, rotas
nginx a 5001 y apagas 5000.

```bash
sudo systemctl start project-api@5001
curl -i http://127.0.0.1:5001/health/live   # 200 OK
sudo vim /etc/nginx/sites-available/project.conf   # cambiar server 127.0.0.1:5000 -> 5001
sudo systemctl reload nginx
sudo systemctl stop project-api@5000   # o elimina la instancia anterior
```

## Logs

- API: `journalctl -u project-api -f` o `tail -F /opt/project/logs/*.log`.
- nginx: `tail -F /var/log/nginx/project.access.log`.
- Correlación por `traceId` (`Serilog` lo incluye en cada log).