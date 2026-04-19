#!/bin/bash
# Run Playwright browser tests against the CloudSoft.Auth.Web app.
# Usage: ./run-playwright-tests.sh [headless|headed]
# Optional env: IDENTITY_STORE=InMemory|SQLite (default: whatever appsettings.json says)

set -e

MODE="${1:-headless}"
BASE_URL="http://localhost:5017"
APP_PID=""

cleanup() {
    if [ -n "$APP_PID" ]; then
        echo "Stopping app (PID: $APP_PID)..."
        kill "$APP_PID" 2>/dev/null || true
        wait "$APP_PID" 2>/dev/null || true
    fi
}
trap cleanup EXIT

echo "Starting CloudSoft.Auth.Web..."
dotnet run --project src/CloudSoft.Auth.Web --launch-profile http > /tmp/cloudsoft-auth.log 2>&1 &
APP_PID=$!

echo "Waiting for app at $BASE_URL..."
for i in $(seq 1 30); do
    if curl -sf "$BASE_URL" > /dev/null 2>&1; then
        echo "App is ready!"
        break
    fi
    if [ "$i" -eq 30 ]; then
        echo "ERROR: App failed to start within 30 seconds"
        echo "--- tail of app log ---"
        tail -40 /tmp/cloudsoft-auth.log || true
        exit 1
    fi
    sleep 1
done

# Warm Razor compilation so the first Playwright test doesn't race cold compile.
echo "Warming up..."
for path in "" "/WhoAmI" "/Account/Login"; do
    curl -sf "$BASE_URL$path" > /dev/null 2>&1 || true
done

echo "Running Playwright tests (mode: $MODE)..."
PLAYWRIGHT_MODE="$MODE" PLAYWRIGHT_BASE_URL="$BASE_URL" \
    dotnet test tests/CloudSoft.Auth.Web.PlaywrightTests/ \
    --logger "console;verbosity=normal"
TEST_EXIT=$?

echo "Tests completed with exit code: $TEST_EXIT"
exit $TEST_EXIT
