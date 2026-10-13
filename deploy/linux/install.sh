#!/usr/bin/env bash
# Installs the published API and migration worker on Ubuntu/Debian or RHEL with systemd.
# Run from the folder that contains the two publish outputs:
#   dotnet publish src/Host/Project.Api -c Release -o out/api
#   dotnet publish src/Host/Project.MigrationService -c Release -o out/migrations
#   sudo ./deploy/linux/install.sh out
set -euo pipefail

SOURCE="${1:?Usage: install.sh <publish-folder containing api/ and migrations/>}"
HERE="$(cd "$(dirname "$0")" && pwd)"

id project >/dev/null 2>&1 || useradd --system --no-create-home --shell /usr/sbin/nologin project
install -d -o project -g project /opt/project/api /opt/project/migrations /var/log/project
install -d -m 750 /etc/project

rsync -a --delete "$SOURCE/api/" /opt/project/api/
rsync -a --delete "$SOURCE/migrations/" /opt/project/migrations/
chown -R project:project /opt/project

if [[ ! -f /etc/project/api.env ]]; then
  install -m 600 "$HERE/api.env.example" /etc/project/api.env
  echo "Edit /etc/project/api.env with the real secrets, then run this script again."
  exit 1
fi

install -m 644 "$HERE/project-api.service" /etc/systemd/system/project-api.service
install -m 644 "$HERE/project-migrations.service" /etc/systemd/system/project-migrations.service
systemctl daemon-reload

systemctl start project-migrations
systemctl enable --now project-api
systemctl restart project-api

echo "OK: project-api is running. Check it with: curl -fsS http://127.0.0.1:5000/health/ready"
