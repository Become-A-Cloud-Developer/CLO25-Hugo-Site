#!/bin/bash
# Run Playwright tests with IdentityStore:Provider = SQLite.
# Wipes any existing cloudsoft-auth.db so seeding starts from a clean state.
# Usage: ./run-playwright-tests-sqlite.sh [headless|headed]

set -e

MODE="${1:-headless}"
BASE_URL="http://localhost:5017"
APP_PID=""

cleanup() {
    if [ -n "$APP_PID" ]; then
        kill "$APP_PID" 2>/dev/null || true
        wait "$APP_PID" 2>/dev/null || true
    fi
}
trap cleanup EXIT

echo "Wiping any stale SQLite DB..."
rm -f src/CloudSoft.Auth.Web/cloudsoft-auth.db src/CloudSoft.Auth.Web/cloudsoft-auth.db-*

echo "Starting CloudSoft.Auth.Web (SQLite)..."
IdentityStore__Provider=SQLite \
    dotnet run --project src/CloudSoft.Auth.Web --launch-profile http \
    > /tmp/cloudsoft-auth-sqlite.log 2>&1 &
APP_PID=$!

echo "Waiting for app at $BASE_URL..."
for i in $(seq 1 30); do
    if curl -sf "$BASE_URL" > /dev/null 2>&1; then
        echo "App is ready!"
        break
    fi
    if [ "$i" -eq 30 ]; then
        echo "ERROR: App failed to start within 30 seconds"
        tail -60 /tmp/cloudsoft-auth-sqlite.log || true
        exit 1
    fi
    sleep 1
done

# First-run Razor compilation can make the initial request slow enough to
# race with the first Playwright test. Warm up a few endpoints so the JIT
# and runtime caches are populated before the browser arrives.
echo "Warming up..."
for path in "" "/WhoAmI" "/Account/Login" "/Diagnostics/Store"; do
    curl -sf "$BASE_URL$path" > /dev/null 2>&1 || true
done

echo "Running Playwright tests (mode: $MODE, store: SQLite)..."
PLAYWRIGHT_MODE="$MODE" PLAYWRIGHT_BASE_URL="$BASE_URL" \
    IDENTITY_STORE_UNDER_TEST=SQLite \
    dotnet test tests/CloudSoft.Auth.Web.PlaywrightTests/ \
    --logger "console;verbosity=normal"
TEST_EXIT=$?

echo "Tests completed with exit code: $TEST_EXIT"
exit $TEST_EXIT
