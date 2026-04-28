# CloudSoft-Api

Reference project for the ACD course's **REST API and DTOs** chapter under `content/exercises/4-services-and-apis/1-rest-api-and-dtos/`.

## Purpose

A small ASP.NET Core Web API called `CloudCiApi` that exposes a quotes endpoint at `/api/quotes`, then progressively gates with API-key middleware, then JWT bearer authentication. The visible cue students can see propagating through the exercises is the Swagger UI: hitting `/swagger` shows the operations grouped under `Quotes` (and later `Tokens`), while hitting `/api/quotes` returns the seed array — first openly, then only with the right `X-Api-Key` header, then only with a signed JWT in the `Authorization` header. Each authentication change is observable both at the wire (curl) and in Swagger UI's Authorize dialog.

- **Exercise 1** — Scaffold the controllers-based Web API, separate DTO and entity types, wire Swagger, deploy via the OIDC-federated pipeline pattern from the deployment chapter, layer Application Insights via the secretref pattern.
- **Exercise 2** — Add an `ApiKeyMiddleware` that demands `X-Api-Key`. Production key delivered via Container Apps secret + secretref env var. Swagger UI's Authorize button wired to the API-key scheme.
- **Exercise 3** — Replace the API key with JWT bearer authentication. `TokensController` issues signed JWTs from an in-memory user list; `[Authorize]` enforces the new identity model. Tear down the resource group and the Entra OIDC app.

## Layout

```text
reference/CloudSoft-Api/
├── src/CloudCiApi/                     # The Web API (final post-Ex 6.3 state)
│   ├── CloudCiApi.csproj
│   ├── Program.cs
│   ├── Controllers/QuotesController.cs # [Authorize]-gated quotes resource
│   ├── Controllers/TokensController.cs # POST /api/tokens/login → JWT
│   ├── Models/Quote.cs                 # domain entity
│   ├── Models/User.cs                  # in-memory user record
│   ├── Dtos/QuoteDto.cs                # output wire shape
│   ├── Dtos/CreateQuoteRequest.cs      # input wire shape with validation
│   ├── Services/IQuoteStore.cs
│   ├── Services/InMemoryQuoteStore.cs
│   ├── Services/IUserStore.cs
│   ├── Services/InMemoryUserStore.cs
│   ├── Dockerfile                      # multi-stage build (.NET 10 SDK → ASP.NET runtime, port 8080)
│   ├── .dockerignore
│   └── appsettings.Development.json    # dev-only Jwt:* values + Logging
├── .github/workflows/ci.yml            # Final OIDC-authenticated pipeline (smoke test scoped to /swagger)
├── scripts/
│   ├── validate.mjs                    # Playwright capture of /swagger + authorised call
│   └── package.json
├── docs/
│   ├── EXERCISE-VALIDATION-REPORT.md   # Live-execution validation record
│   ├── screenshots/                    # week-6-swagger.png, week-6-authorised-call.png
│   └── validation/                     # week-6-curl-matrix.txt, week-6-app-insights-output.txt
├── CLAUDE.md
└── README.md
```

The `ApiKeyMiddleware.cs` file from Exercise 2 is **not** present — Exercise 3 deletes both the registration in `Program.cs` and the file itself, since the cleaner story for the chapter's arc is *replace* rather than *layer*.

## Running locally

```bash
cd src/CloudCiApi
dotnet run
```

The app prints the port at startup (typically `http://localhost:5XXX`). Visit `/swagger` to see the operations and use the **Authorize** button to paste a token, or use `curl` directly. The dev-mode JWT signing key in `appsettings.Development.json` is what the issuing `TokensController` and the validating bearer handler both read — local tokens validate locally without any further setup.

```bash
# Acquire a token
curl -s -X POST http://localhost:5XXX/api/tokens/login \
  -H "Content-Type: application/json" \
  -d '{"username":"alice","password":"alice123"}'

# Use the token
TOKEN=<paste>
curl http://localhost:5XXX/api/quotes -H "Authorization: Bearer $TOKEN"
```

Two seeded users live in `Services/InMemoryUserStore.cs`: `alice / alice123` (role `admin`) and `bob / bob456` (role `reader`).

## Building the container locally

```bash
cd src/CloudCiApi
docker build -t cloudci-api:local .
docker run --rm -p 8080:8080 cloudci-api:local
```

The local container has no `Jwt__SigningKey` env var, so it falls back to the dev value baked into `appsettings.Development.json` only if `ASPNETCORE_ENVIRONMENT=Development` is also set. Otherwise the JWT validator has no key and every authenticated request returns `401`. To test the full bearer flow against the local image, run with `-e ASPNETCORE_ENVIRONMENT=Development`. The deployed Container App injects all three `Jwt__*` env vars (with `Jwt__SigningKey` via `secretref:`) so the production path doesn't depend on Development-mode fallbacks.

## Exercise progression

Each exercise corresponds to one or more commits in the live GitHub repository. The state in this directory represents the **final** state after all three exercises are complete: JWT bearer authentication, `TokensController`, the API-key middleware retired, Swagger Authorize dialog using the bearer scheme, and the deployment torn down.

## Live deployment

See `docs/EXERCISE-VALIDATION-REPORT.md` for the live URL, resource names, GitHub Actions run links, and manual verification steps.

## Validation

Smoke-check the live deployment with the included Playwright script:

```bash
cd scripts
npm install
FQDN=<container-app-fqdn> TOKEN=<fresh-jwt> node validate.mjs
```

The script loads `https://$FQDN/swagger`, asserts HTTP 200, takes a full-page screenshot (`docs/screenshots/week-6-swagger.png`), then walks the Swagger UI Authorize flow with the supplied JWT and screenshots the authenticated `GET /api/quotes` response (`docs/screenshots/week-6-authorised-call.png`). The curl matrix and the App Insights query transcripts in `docs/validation/` are produced by the chapter-development run; re-run those probes manually against any future re-deployment.
