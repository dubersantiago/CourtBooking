#!/bin/bash
set -euxo pipefail

# Swap de 2 GB: la VM tiene solo 1 GB de RAM
fallocate -l 2G /swapfile
chmod 600 /swapfile
mkswap /swapfile
swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab

# Docker
dnf install -y docker
systemctl enable --now docker

# Plugin "docker compose" (Amazon Linux no lo trae en sus repositorios)
mkdir -p /usr/local/lib/docker/cli-plugins
curl -sSL "https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64" \
  -o /usr/local/lib/docker/cli-plugins/docker-compose
chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

# Carpeta donde vivirán el compose y el .env de la app
mkdir -p /opt/courtbooking