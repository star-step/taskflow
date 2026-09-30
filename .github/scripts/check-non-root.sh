#!/usr/bin/env bash
# Fails if any application container's main process runs as root (uid 0).
# Usage: .github/scripts/check-non-root.sh [service...]   (default: api web)
set -euo pipefail

status=0
for service in "${@:-api web}"; do
  for s in $service; do
    uid=$(docker compose exec -T "$s" id -u)
    user=$(docker compose exec -T "$s" id -un 2>/dev/null || echo "?")
    if [ "$uid" = "0" ]; then
      echo "FAIL: $s runs as root (uid 0)"
      status=1
    else
      echo "ok: $s runs as $user (uid $uid)"
    fi
  done
done
exit $status
