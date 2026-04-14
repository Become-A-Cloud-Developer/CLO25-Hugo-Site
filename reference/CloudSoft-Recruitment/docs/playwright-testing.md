# Playwright Browser Testing

## Purpose

Playwright tests complement the existing WebApplicationFactory integration tests by running in a real browser. Use cases:
- **Demos**: Show the app working with `slowmo` mode during lectures
- **Assignment verification**: Quickly validate student submissions with headed mode
- **Instructor tooling**: AI-driven exploration via Playwright MCP
- **Curriculum content**: Potential future teaching material for browser testing

## Setup

```bash
# Build the test project (restores NuGet packages)
dotnet build tests/CloudSoft.Web.PlaywrightTests/

# Install Playwright browsers (one-time)
pwsh tests/CloudSoft.Web.PlaywrightTests/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Running Scripted Tests

The app must be running before executing tests. Tests auto-skip if the app isn't reachable.

### Quick start with helper script

```bash
./run-playwright-tests.sh              # headless (CI-friendly)
./run-playwright-tests.sh headed       # visible browser
./run-playwright-tests.sh slowmo       # visible + 500ms delay (great for demos)
```

### Manual

```bash
# Terminal 1: Start the app
dotnet run --project src/CloudSoft.Web

# Terminal 2: Run tests
PLAYWRIGHT_MODE=headless dotnet test tests/CloudSoft.Web.PlaywrightTests/
PLAYWRIGHT_MODE=headed dotnet test tests/CloudSoft.Web.PlaywrightTests/
PLAYWRIGHT_MODE=slowmo dotnet test tests/CloudSoft.Web.PlaywrightTests/
```

### Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PLAYWRIGHT_MODE` | `headless` | `headless`, `headed`, or `slowmo` (headed + 500ms delay) |
| `PLAYWRIGHT_BASE_URL` | `http://localhost:5161` | Base URL of the running app |

## Test Coverage

| Test | What it verifies |
|------|-----------------|
| HomePage_ShowsCloudSoftTitle | Title and heading load correctly |
| JobListing_IsAccessible | /Job page loads with heading |
| JobCreate_RedirectsToLogin | Unauthenticated access redirects to login |
| AdminJourney_CreateEditDeleteJob | Full CRUD lifecycle as admin |
| CandidateJourney_ApplyToJob | Apply flow and My Applications |
| CandidateCannotAccessAdmin | Role enforcement (403/redirect) |

## Using Playwright MCP

The project includes a Playwright MCP server configuration (`.claude/settings.json`). This lets Claude Code interactively browse the app.

### Snapshot mode (default)

Uses accessibility tree — fast and reliable:

> "Navigate to http://localhost:5161 and log in as admin@cloudsoft.com with password Admin123!"
> "Create a new job posting for a Cloud Engineer in Bergen"
> "Log out and log in as candidate@test.com, then apply to the job"

### Vision mode

For visual verification, add `"--vision"` to the args in `.claude/settings.json`:

```json
{
  "mcpServers": {
    "playwright": {
      "command": "npx",
      "args": ["@playwright/mcp@latest", "--vision"]
    }
  }
}
```

## Comparison: Testing Approaches

| Aspect | WebApplicationFactory | Playwright Scripted | Playwright MCP |
|--------|----------------------|--------------------|--------------------|
| Speed | Fast (~2s) | Medium (~15s) | Interactive |
| Browser | No (HTTP only) | Yes (Chromium) | Yes (Chromium) |
| JS/CSS | Not tested | Fully rendered | Fully rendered |
| Setup | None | Install browsers | Install browsers + Node.js |
| Best for | CI, regression | E2E verification, demos | Exploration, ad-hoc testing |
| Runs in CI | Yes | Yes (headless) | No (interactive) |

## Architecture Notes

- **No ProjectReference**: Playwright tests are fully decoupled from the app. They connect over HTTP, just like a real user.
- **Auto-skip**: If the app isn't running, tests skip gracefully instead of failing. This means `dotnet test CloudSoft.slnx` always works.
- **Shared browser**: One Chromium instance shared across all tests via xUnit collection fixture. Each test gets an isolated browser context (separate cookies/state).
- **Unique test data**: Job titles include a GUID suffix to avoid collisions with existing in-memory data.
