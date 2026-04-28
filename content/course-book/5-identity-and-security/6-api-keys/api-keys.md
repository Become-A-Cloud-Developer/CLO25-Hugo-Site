+++
title = "API Keys and Machine-to-Machine"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 60
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/5-identity-and-security/6-api-keys.html)

[Se presentationen på svenska](/presentations/course-book/5-identity-and-security/6-api-keys-swe.html)

---

A backend service that calls another backend service has no human at a keyboard to type a password and no browser session to carry a cookie. The caller is a process; the callee still needs to decide whether to answer. The cheapest credential for that scenario is a long random string the two services agree on in advance, presented in a request header on every call. What such a string proves about the caller, how it travels with the request, where ASP.NET Core enforces the check, and where the mechanism stops being enough are the questions that follow.

## The machine-to-machine problem

Browser-based authentication has a user. The user types credentials into a login form, the server validates them against a user store, and the server issues a session credential — usually a cookie — that the browser attaches automatically to subsequent requests. Every chapter so far has built on that arc.

Service-to-service traffic has none of those pieces. A scheduled job calls a reporting API; a webhook handler in one service calls a fulfilment endpoint in another; a frontend BFF calls a downstream service it owns. There is no login form to render, no human to type a password, no browser to hold a cookie. The caller is a process started by an orchestrator, identified only by what it can prove on the wire.

This is the *machine-to-machine* (M2M) case, and the canonical credential for it is the API key. An **API key** is a long, opaque shared secret a client presents in a request header (commonly `X-Api-Key`) to identify itself to the server; unlike a JWT it carries no claims, and unlike a user password it identifies a calling application rather than a person. The server compares the presented value against a configured expected value and either accepts the request or rejects it. There is no user, no role, no expiration baked into the credential — only "the caller knows the shared key, or it does not."

That bare property is the entire security model. Everything else in this chapter follows from it.

## How an API key travels with a request

An API key is just bytes — a string of letters, digits, and base64 punctuation, typically 256 bits or more of entropy. It travels in an [HTTP header](/course-book/3-application-development/1-http-fundamentals/) named by the service. The de facto convention is `X-Api-Key`, though some APIs use `Authorization: ApiKey <value>` or a custom name. A typical request looks like this:

```text
GET /api/quotes HTTP/1.1
Host: ca-api-week6.azurecontainerapps.io
X-Api-Key: kQ8j+7Hf4N2lZtwBpY5RxV9aMcDeGhJkLnPqSt...
Accept: application/json
```

The server reads the header, compares it against the expected value, and either lets the request proceed or short-circuits with an error status. Two failure modes need separate codes. *No header at all* means the server cannot identify the caller, so the response is `401 Unauthorized` with a `WWW-Authenticate: ApiKey` header naming the expected scheme. *A header that does not match* means the server can identify the caller's claim of authentication and refuses it, so the response is `403 Forbidden`. RFC 7235 is precise about that split, and both numeric responses are mechanical for an API key: missing → 401, wrong → 403, right → 200.

Because the entire credential travels in a header on every request, two operational requirements follow. The transport must be TLS — anything that crosses the wire in plaintext puts the key in every intermediate router's logs. The key itself must be long enough that brute-forcing it across the network is implausible; 256 bits of entropy is the standard floor.

## ASP.NET Core middleware as the enforcement point

ASP.NET Core processes a request as an ordered chain of middleware components. Each component receives an `HttpContext` and a delegate to the next link; it can inspect the request, write a short-circuit response, or call the delegate to hand off. Authentication, authorization, routing, response compression, and exception handling are all middleware. An API-key check is exactly that shape: read the header, compare it to a configured value, either short-circuit with 401/403 or call `await _next(context)`.

Custom middleware in ASP.NET Core is a class with two requirements: a constructor that takes a `RequestDelegate` (the next link in the chain), and an `InvokeAsync(HttpContext)` method that does the work. The class does not implement an interface — the framework discovers the shape by convention when `app.UseMiddleware<T>()` is called. Constructor parameters beyond `RequestDelegate` are resolved from dependency injection at construction time, which is fine for a configuration-bound value like the expected API key.

### A worked example

The companion exercise [Securing the API with an API Key Middleware](/exercises/4-services-and-apis/1-rest-api-and-dtos/) walks the full pattern end to end. The middleware itself is short:

```csharp
public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private const string ConfigKey = "ApiKey:Value";

    private readonly RequestDelegate _next;
    private readonly string? _expectedKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _expectedKey = configuration[ConfigKey];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey)
            || string.IsNullOrWhiteSpace(providedKey))
        {
            context.Response.Headers.Append("WWW-Authenticate", "ApiKey");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (string.IsNullOrEmpty(_expectedKey)
            || !string.Equals(providedKey, _expectedKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
```

Three branches cover the three outcomes. The constructor reads `ApiKey:Value` from `IConfiguration` once at startup; the per-request method only compares strings. The comparison is `StringComparison.Ordinal` — culture-aware comparison can normalize Unicode in ways that accept the wrong key, and an API key is a byte-for-byte token, not human-readable text. Registration in `Program.cs` is one line, placed after routing-related middleware and before the endpoint terminus:

```csharp
app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();
```

That position runs the check after routing has matched a path to an endpoint, so later evolutions can inspect endpoint metadata to exempt specific routes (a `/health` probe, the Swagger UI), and before any controller code runs on a rejected request.

## Why API keys are weaker than JWT

An API key answers exactly one question: does the caller know the shared secret? Compare that to a [JWT bearer token](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/), and three weaknesses emerge.

A JWT carries an *expiration* in its `exp` claim. The validating service rejects tokens past their lifetime without any operational action. An API key has no built-in expiration. It is valid until somebody changes the configured value on both sides, which is a deployment, not a token-validation step.

A JWT carries [claims](/course-book/5-identity-and-security/4-roles-claims-and-policies/) — subject, roles, tenant, custom application data — signed by the issuer. The validating service can authorize on the contents without consulting another database. An API key carries no claims. Every caller that holds the key looks identical to the server; the server cannot distinguish the scheduled job from the webhook handler from the developer running curl with the production key on a laptop. If different callers need different permissions, an API key cannot express the difference.

A JWT supports *rotation through key IDs*. The issuer can publish multiple signing keys with different `kid` headers; the validator accepts any token signed by any currently-published key. Old tokens stay valid until they expire; new tokens are signed with the new key; the cutover is invisible to clients. An API key has no equivalent. Rotating it means replacing the configured value on the server, which invalidates every client at once unless the server is built to accept multiple valid keys during a transition window.

| Property | API key | JWT bearer token |
|----------|---------|------------------|
| Expiration | None inherent | `exp` claim, validated automatically |
| Identity carried | None — only "knows the secret" | Subject, claims, scopes |
| Per-caller authorization | No — same key, same access | Yes — claims drive `[Authorize]` decisions |
| Rotation cost | Coordinated cutover for every client | New key ID; old tokens age out |
| Wire format | Opaque string in `X-Api-Key` | Signed `header.payload.signature` in `Authorization: Bearer` |
| Best fit | One trusted client, simple gate | Multiple callers, per-caller permissions |

The right choice depends on what the gate actually needs to enforce. When the threat model is "keep the open internet from hammering this endpoint" and there is one trusted client behind it, an API key is sufficient and simpler. When the threat model is "different callers get different access" or "credentials must expire automatically," JWT is the next step.

## Generating, storing, and rotating keys

The credential's strength depends entirely on operational discipline. Three practices carry most of that weight.

### Cryptographically random generation

A predictable key is no key. The generator must be cryptographically random — no incrementing counters, no timestamp-derived strings, no UUIDs (which include non-random bits). On a development machine the canonical command is:

```bash
openssl rand -base64 48
```

That produces 48 random bytes (384 bits) of entropy, base64-encoded into a 64-character string. The base64 alphabet is wire-friendly: no spaces, no quoting issues, safe in HTTP headers and shell commands. The exact length above 256 bits is mostly insurance against future cryptanalysis; the practical exposure surface for an API key is leakage, not brute force. A key that ends up in a Slack channel or a screenshotted terminal window is compromised regardless of how many bits it had.

### Storage out of source control

A key checked into the repository is no longer secret. Anyone who clones the repo — including everyone who ever forks it, plus every CI job, plus every static-analysis service that mirrors source — has the credential. Two correct homes exist for the configured value, depending on environment:

| Environment | Storage | Why |
|-------------|---------|-----|
| Local development | Non-secret placeholder in `appsettings.Development.json`, or `dotnet user-secrets` for real local credentials | Loopback traffic on a developer machine has no realistic attacker; a clearly labelled placeholder is self-documenting if it leaks |
| Production | Platform secret store, injected as an environment variable via a `secretref:` mechanism | Encrypted at rest, separate permission to read, never visible in `az containerapp show` or Portal blades |

Production secrets belong in a secret store — [Azure Key Vault](/course-book/5-identity-and-security/8-secret-management/) for general-purpose secret management, or the Container Apps secret mechanism for Container Apps deployments. The application code reads the same `IConfiguration` key (`ApiKey:Value`) in both environments; only the surrounding provider chain differs. That uniformity is the point of the configuration system: one read, three sources, one mental model.

### Key rotation

**Key rotation** is the practice of issuing a new credential and retiring the old one on a schedule (or after a suspected compromise); rotation hardens systems against long-lived stolen credentials and is supported by API gateways and secret stores that allow multiple valid keys during a transition window.

For a single-key configuration the rotation is destructive: change the secret on the server, every client breaks until it picks up the new value, then traffic resumes. That is acceptable when the client and server are deployed together and one of them owns both ends. It is not acceptable when independent clients consume the API on schedules the server cannot coordinate.

For independent clients the configuration must accept two valid keys at once. The server reads both `ApiKey:Current` and `ApiKey:Previous` and accepts a match against either. Rotation then has three steps: write the new key as `Current` and demote the old one to `Previous`; let clients pick up the new value at their own pace; once telemetry shows no traffic still using the old key, remove `Previous`. This window pattern is what API gateways implement as a managed service, and what secret stores expose as secret versioning.

A rotation discipline is what distinguishes a real production deployment from a demo. Without rotation, the same key ends up in shell history, deployment scripts, debug logs, and an old laptop somewhere over a long enough timeline. With rotation, the blast radius of any individual leak is bounded by the rotation period.

## When an API key is enough — and when it is not

The honest summary of the threat model is short. An API key proves that some client holds the shared secret; it says nothing about which client. Every client with the key looks identical to the server, and rotating the key invalidates every client at once unless the server runs the dual-key pattern.

That property is sufficient in three situations. A single trusted client calls the API, deployed alongside the server, both rotated together. A small number of trusted clients call the API and per-caller authorization is not needed — the gate exists to keep anonymous traffic out, not to differentiate callers. A development or internal API exposes data that is not sensitive enough to warrant per-user attribution, and the operational simplicity of one shared credential outweighs the audit cost.

The property is insufficient when per-caller attribution matters (which user did this?), when callers need different permissions (read versus write, scope A versus scope B), when credentials must expire automatically without operator action, or when the API is consumed by multiple independent organizations that must be revoked individually. For those cases the next step up is a JWT bearer token, possibly issued by an OAuth 2.0 authorization server. The transport mechanism is the same — a header on every request — but the credential carries identity and the validating service authorizes per-caller.

The architectural pattern is to start with the simplest gate that matches the threat model, and to escalate when a real requirement appears that the simpler gate cannot meet. An API key is rarely the wrong first step; it is often the wrong forever step.

## Summary

A backend service calling another backend service has no user to authenticate, so it presents a long random shared secret in a request header — an API key — that identifies the calling application. The header conventionally named `X-Api-Key` travels with every request over TLS, and a small ASP.NET Core middleware enforces the check by reading the header, comparing it to a configured expected value, and short-circuiting with 401 (no header) or 403 (wrong header) when the comparison fails. API keys are weaker than JWT bearer tokens because they carry no expiration, no claims, and no built-in rotation — every caller with the key looks identical to the server. Operational discipline closes that gap: generate keys with a cryptographically random source of at least 256 bits, store them in a platform secret store rather than source control, and rotate them on a schedule using a dual-key window so independent clients can cut over without downtime. The mechanism is sufficient for one-trusted-client machine-to-machine traffic and insufficient when per-caller attribution or automatic expiration is required, at which point JWT is the natural next step.
