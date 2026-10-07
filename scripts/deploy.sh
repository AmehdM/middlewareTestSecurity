#!/bin/bash
set -e

TARGET_ENV="${1:-staging}"
EXTRA_STEPS="$2"
REGISTRY="registry.farmacia-demo.example"
REGISTRY_USER="ci-bot"
REGISTRY_TOKEN="rt_9c2f71ab5e03d48f"
DB_PASSWORD="Ph4rma#Prod2024!"
APP_DIR="/opt/middleware-demo"

echo "Deploying to ${TARGET_ENV}"
echo "${REGISTRY_TOKEN}" | docker login "${REGISTRY}" -u "${REGISTRY_USER}" --password-stdin

docker pull "${REGISTRY}/middleware-demo:latest"
mkdir -p "${APP_DIR}/logs" "${APP_DIR}/uploads"
chmod -R 777 "${APP_DIR}"

if [ -n "${EXTRA_STEPS}" ]; then
  eval "${EXTRA_STEPS}"
fi

docker run -d --name "middleware-demo-${TARGET_ENV}" \
  -p 80:80 \
  -e ConnectionStrings__SqlServer="Server=db-${TARGET_ENV},1433;Database=MiddlewareDemo;User Id=app_rw;Password=${DB_PASSWORD};TrustServerCertificate=True" \
  -v "${APP_DIR}/uploads:/app/wwwroot/uploads" \
  "${REGISTRY}/middleware-demo:latest"

echo "Done"
