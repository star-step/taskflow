#!/usr/bin/env bash
# End-to-end smoke test against a running stack (docker compose up).
# Usage: .github/scripts/smoke-test.sh [base-url]   (default http://localhost:8080)
# Reads the demo account from .env.
set -euo pipefail

BASE="${1:-http://localhost:8080}"
# .env is created at runtime from .env.example, so shellcheck can't follow it.
set -a
# shellcheck disable=SC1091
source .env
set +a

fail() { echo "FAIL: $*" >&2; exit 1; }
json() { python3 -c "import sys, json; print(json.load(sys.stdin)$1)"; }

echo "1. Anonymous request is rejected with ProblemDetails"
code=$(curl -s -o /tmp/unauth.json -w '%{http_code}' "$BASE/api/tasks")
[ "$code" = 401 ] || fail "expected 401, got $code"
[ "$(json "['errorCode']" < /tmp/unauth.json)" = UNAUTHENTICATED ] || fail "missing errorCode"

echo "2. Demo user can sign in"
token=$(curl -sf -H 'Content-Type: application/json' \
  -d "{\"email\":\"$DEMO_USER_1_EMAIL\",\"password\":\"$DEMO_USER_1_PASSWORD\"}" \
  "$BASE/api/auth/login" | json "['accessToken']") || fail "login failed"
auth=(-H "Authorization: Bearer $token")

echo "3. Seeded tasks are listed (soft-deleted row excluded)"
count=$(curl -sf "${auth[@]}" "$BASE/api/tasks" | json ".__len__()")
[ "$count" -ge 12 ] || fail "expected at least 12 tasks, got $count"

echo "4. Raw-SQL summary agrees with the list"
total=$(curl -sf "${auth[@]}" "$BASE/api/tasks/summary" | json "['total']")
[ "$total" = "$count" ] || fail "summary total $total != list count $count"

echo "5. Create (Location header points back through the proxy), change status, soft-delete"
created=$(curl -sf -D /tmp/create-headers.txt "${auth[@]}" -H 'Content-Type: application/json' \
  -d '{"title":"CI smoke test","priority":"Low"}' "$BASE/api/tasks")
id=$(json "['id']" <<<"$created")
location=$(grep -i '^location:' /tmp/create-headers.txt | tr -d '\r' | cut -d' ' -f2)
[ "$location" = "$BASE/api/tasks/$id" ] || fail "Location header '$location', expected '$BASE/api/tasks/$id'"
status=$(curl -sf -X PATCH "${auth[@]}" -H 'Content-Type: application/json' \
  -d '{"status":"Done"}' "$BASE/api/tasks/$id/status" | json "['status']")
[ "$status" = Done ] || fail "status change returned $status"
[ "$(curl -s -o /dev/null -w '%{http_code}' -X DELETE "${auth[@]}" "$BASE/api/tasks/$id")" = 204 ] || fail "delete"
[ "$(curl -s -o /dev/null -w '%{http_code}' "${auth[@]}" "$BASE/api/tasks/$id")" = 404 ] || fail "deleted task still visible"

echo "6. SPA is served with security headers"
headers=$(curl -sfI "$BASE/login")
grep -qi '^content-security-policy:' <<<"$headers" || fail "missing CSP header"
grep -qi '^x-frame-options: DENY' <<<"$headers" || fail "missing X-Frame-Options"

echo "Smoke test passed ($count tasks)."
