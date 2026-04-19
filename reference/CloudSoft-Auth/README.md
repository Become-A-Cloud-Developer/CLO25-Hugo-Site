# CloudSoft-Auth

Reference project for the ACD course's **Authentication and Authorization** exercises (Chapter 4) and **Identity and User Stores** exercises (Chapter 5) under `content/exercises/10-webapp-development/`.

## Purpose

A tiny ASP.NET Core MVC application that grows alongside the exercises. The single laboratory surface is a **"Who Am I?"** page linked from the footer. Each exercise extends what the page reveals about the current user.

- Chapter 4 (Exercises 1–4): hardcoded users, cookie authentication, claims, roles, policies, CSRF, Google OIDC. No database, no ASP.NET Core Identity.
- Chapter 5 (Exercises 1–4): migrate to ASP.NET Core Identity, feature-flagged InMemory/SQLite user store, startup admin seeding, registration and role promotion.

## Layout

```text
reference/CloudSoft-Auth/
├── src/CloudSoft.Auth.Web/             # The MVC application
├── tests/CloudSoft.Auth.Web.PlaywrightTests/   # End-to-end Playwright tests
└── run-playwright-tests.sh             # Starts the app, runs tests, cleans up
```

## Running locally

```bash
cd src/CloudSoft.Auth.Web
dotnet run --launch-profile http
```

The app serves on `http://localhost:5017`. Visit **Who Am I?** in the footer.

## Running the end-to-end tests

```bash
./run-playwright-tests.sh headless
```

Prerequisites: `dotnet playwright install chromium` once per machine.

## Exercise progression

Each exercise corresponds to one commit in the repository history. The commit message identifies the exercise (for example: `Reference: Ex 4.1 complete — cookie auth + WhoAmI`). Tests accumulate with each exercise, so the full suite exercises every stage up to and including the current HEAD.
