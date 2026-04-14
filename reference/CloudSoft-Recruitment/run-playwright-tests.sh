#!/bin/bash
# Run Playwright browser tests against the CloudSoft app
# Usage: ./run-playwright-tests.sh [headless|headed|slowmo]

set -e

MODE="${1:-headless}"
BASE_URL="http://localhost:5161"
SLOWMO=2000
APP_PID=""

cleanup() {
    if [ -n "$APP_PID" ]; then
        echo "Stopping app (PID: $APP_PID)..."
        kill "$APP_PID" 2>/dev/null || true
        wait "$APP_PID" 2>/dev/null || true
    fi
}
trap cleanup EXIT

echo "Starting CloudSoft.Web..."
dotnet run --project src/CloudSoft.Web --launch-profile http &
APP_PID=$!

echo "Waiting for app at $BASE_URL..."
for i in $(seq 1 30); do
    if curl -sf "$BASE_URL" > /dev/null 2>&1; then
        echo "App is ready!"
        break
    fi
    if [ "$i" -eq 30 ]; then
        echo "ERROR: App failed to start within 30 seconds"
        exit 1
    fi
    sleep 1
done

echo "Running Playwright tests (mode: $MODE)..."
PLAYWRIGHT_MODE="$MODE" PLAYWRIGHT_SLOWMO="$SLOWMO" PLAYWRIGHT_BASE_URL="$BASE_URL" dotnet test tests/CloudSoft.Web.PlaywrightTests/ --logger "console;verbosity=detailed"
TEST_EXIT=$?

echo "Tests completed with exit code: $TEST_EXIT"
exit $TEST_EXIT
