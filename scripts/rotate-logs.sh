#!/bin/bash
set -euo pipefail

LOG_DIR="${LOG_DIR:-/var/log/middleware-demo}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"

if [ ! -d "${LOG_DIR}" ]; then
  echo "Log directory not found: ${LOG_DIR}" >&2
  exit 1
fi

find "${LOG_DIR}" -type f -name '*.log' -mtime +1 -exec gzip -q {} +
find "${LOG_DIR}" -type f -name '*.log.gz' -mtime +"${RETENTION_DAYS}" -delete
echo "Rotation finished for ${LOG_DIR}"
