+++
title = "Bearer Tokens and JWT"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 50
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/5-identity-and-security/5-bearer-tokens-and-jwt.html)

[Se presentationen på svenska](/presentations/course-book/5-identity-and-security/5-bearer-tokens-and-jwt-swe.html)

---

Cookie-based sign-in works because the caller is a browser visiting pages on the same site. Once the caller becomes a mobile app talking to a JSON API, a single-page application calling its backend over CORS, or a backend service calling another backend service, cookies stop fitting. There is no browser to manage the cookie jar, no same-origin domain to scope it to, and often no server-side session store the operators want to maintain. APIs solve this with a different transport for the credential — a token the client carries explicitly in the `Authorization` header — and with a self-describing token format that lets the receiving service validate the credential without touching a session database. Bearer tokens as a concept, the JWT structure that fills the bearer-token slot, the validation path the server runs, and the points at which the model breaks down are what the rest of the chapter develops.

## Why cookies do not fit API calls

Cookie authentication works at the browser layer. When a server sends `Set-Cookie`, the browser stores the value, scopes it to the issuing domain, and attaches it to every subsequent request to that domain — see [Cookie-based authentication](/course-book/5-identity-and-security/2-cookie-authentication/) for the full mechanics. That whole machinery lives in the browser. A native mobile app does not have a cookie jar shared with a browser; a Swift or Kotlin HTTP client must be told explicitly to attach a credential to each request. A single-page application calling an API on a different domain runs into CORS rules and the `SameSite` attribute, both of which were tightened precisely to stop cookies from leaking across origins. A scheduled background job calling a partner API has no browser at all.

What every one of these clients does have is the ability to set an HTTP header on the requests it makes. That is the slot bearer tokens fill. The client obtains a credential through some sign-in flow, then sends it on every API call as an `Authorization` header. The server validates the header, identifies the caller, and decides whether to serve the request — without ever asking the client to maintain a session cookie.

## Bearer tokens

A **bearer token** is an opaque or structured credential a client sends in an HTTP request's `Authorization: Bearer <token>` header; whoever bears the token is treated as authenticated, so bearer tokens must be transmitted over TLS and kept short-lived. The wire format is deliberately simple:

```text
GET /api/quotes HTTP/1.1
Host: api.example.com
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

The word "bearer" is load-bearing. The server does not check who is presenting the token, only that the token itself is valid. Any process that holds the token can use it. That is the design — it is what lets a mobile app, a server-side worker, and a CLI tool all use the same authentication scheme — but it is also the threat model. A leaked token is a usable credential until it expires, with no equivalent of "this is the wrong machine, reject it."

Two consequences fall out of this immediately. First, bearer tokens must travel over TLS. A token sent over plain HTTP can be observed by any intermediary on the network and replayed verbatim; with no possession check beyond holding the bytes, replay is sufficient. Production APIs reject HTTP entirely and accept tokens only over HTTPS. Second, bearer tokens should be short-lived. The shorter the token's lifetime, the smaller the window during which a leaked token is useful. Most API designs issue bearer tokens with lifetimes measured in minutes to a small number of hours, then refresh them through a separate flow.

Bearer tokens themselves do not specify a format. The header could carry a random opaque string that the server looks up in a database. In practice, the structured-token format that has won is the JWT.

## The JWT format

A **JWT** (JSON Web Token) is a structured bearer-token format consisting of a base64-encoded header, payload, and signature joined by dots; the payload carries claims (subject, issuer, audience, expiration), and the signature lets the recipient verify the token has not been tampered with. Decoded into its three parts, a JWT looks like this:

```text
eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6IjEyMyJ9
.
eyJpc3MiOiJodHRwczovL2lkLmV4YW1wbGUuY29tIiwic3ViIjoiNDIiLCJhdWQiOiJhcGkuZXhhbXBsZS5jb20iLCJleHAiOjE3MTM5NjAwMDB9
.
TJ8x8KQ3vH9z...
```

The first segment is the *header*. Decoded as JSON, it tells the recipient which algorithm signed the token and, optionally, which key was used:

```json
{ "alg": "RS256", "typ": "JWT", "kid": "123" }
```

The second segment is the *payload*, sometimes called the claim set. It is JSON describing who the token is about and the constraints under which it is valid:

```json
{
  "iss": "https://id.example.com",
  "sub": "42",
  "aud": "api.example.com",
  "iat": 1713956400,
  "nbf": 1713956400,
  "exp": 1713960000,
  "role": "admin"
}
```

The third segment is the *signature*. It is the cryptographic output of signing `base64url(header) + "." + base64url(payload)` with the issuer's signing key. Verifying that signature with the matching key proves two things: the token came from someone who had the signing key, and not a single byte of the header or payload has been altered since.

An important non-property of the encoding deserves stating plainly: base64url is encoding, not encryption. Anyone who copies a JWT into a debugger can read the payload. The JWT format protects integrity, not confidentiality. Sensitive information — passwords, raw personal data, anything that would harm the user if disclosed — does not belong in a JWT payload.

### The standard claims

The JWT specification reserves a small set of short claim names with defined meanings. They are the spine of every validation routine.

| Claim | Meaning |
|-------|---------|
| `iss` | Issuer — the service that signed the token |
| `sub` | Subject — the identity the token is about, typically a user ID |
| `aud` | Audience — the service the token is intended for |
| `exp` | Expiration time as a Unix timestamp; the token is invalid after this instant |
| `nbf` | Not before — the earliest time at which the token may be used |
| `iat` | Issued at — when the token was minted |

A JWT's **issuer** (`iss` claim) names the service that signed the token, and the **audience** (`aud` claim) names the service that should accept it; the validating service rejects tokens whose issuer or audience do not match its configured expectations, preventing tokens minted for one service from being replayed against another. These two claims do most of the heavy lifting in multi-service architectures. The token a sign-in service mints for the quotes API carries `aud: "https://api.example.com/quotes"`; the quotes API checks the audience and refuses any token where it does not match. A token leaked from a different API in the same fleet cannot be replayed against the quotes API, because its `aud` claim names a different service.

`exp`, `nbf`, and `iat` together bound the token's validity in time. `iat` records when it was issued (useful for logging and for revocation strategies based on "everything before this timestamp is invalid"); `nbf` and `exp` define the window during which the token may be used. Beyond the standard claims, the payload can carry any application-specific [claims](/course-book/5-identity-and-security/4-roles-claims-and-policies/) the issuer wants the recipient to know — role memberships, tenant identifiers, scope strings.

## Validating a JWT

When a request arrives at the API, the server pulls the token out of the `Authorization` header and runs a fixed validation routine before any controller code executes:

1. Split the token into its three segments.
2. Decode the header and read the `alg` and `kid` fields.
3. Look up the verification key for that `kid`.
4. Compute the signature over the first two segments and compare it to the third.
5. Decode the payload and check `iss`, `aud`, and the time-based claims.

If any step fails, the server responds with `401 Unauthorized` and the request never reaches the controller. If every step succeeds, the server constructs a `ClaimsPrincipal` from the payload and the controller sees an authenticated request — exactly as it would after cookie authentication.

The verification key depends on the signing algorithm. Two patterns dominate. *Symmetric signing* uses an HMAC algorithm such as `HS256`, where the same secret signs and verifies. This is straightforward when one service both issues and validates the tokens, but it forces every validating service to hold the signing secret — a problem when many services need to validate tokens from a single issuer. *Asymmetric signing* uses an algorithm such as `RS256` or `ES256`, where the issuer signs with a private key and validators verify with the matching public key. The private key never leaves the issuer; the public key can be published openly, typically at a well-known URL such as `https://id.example.com/.well-known/jwks.json`. This is the model OAuth and OpenID Connect rely on, and the model ASP.NET Core's `JwtBearer` middleware expects when an authority is configured.

### A worked example: the quotes API

The companion exercise [REST API and DTOs](/exercises/4-services-and-apis/1-rest-api-and-dtos/) walks through this pattern by building a small `CloudCiApi` quotes service and progressively adding a JWT bearer gate on top of it. Registering the middleware in `Program.cs` takes a handful of lines:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience  = builder.Configuration["Jwt:Audience"];
        options.TokenValidationParameters.ValidateIssuer   = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateLifetime = true;
    });

builder.Services.AddAuthorization();
```

The `Authority` value points at the issuer (for example `https://id.example.com`). The middleware fetches `/.well-known/openid-configuration` from that URL on startup, reads the `jwks_uri` it advertises, and downloads the public keys it should use to verify signatures. The `Audience` value is the string the middleware will require in the `aud` claim of every incoming token. Everything else — splitting the token, checking `exp` and `nbf`, comparing the signature — is handled automatically.

Controllers opt into the gate with the `[Authorize]` attribute, naming the JWT scheme so it is unambiguous which authentication scheme should validate the request:

```csharp
[ApiController]
[Route("api/quotes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class QuotesController : ControllerBase
{
    [HttpGet]
    public IEnumerable<QuoteDto> List() => _service.GetAll();
}
```

A request without an `Authorization` header now returns `401 Unauthorized` with a `WWW-Authenticate: Bearer` response header. A request with a token that fails any validation step returns `401` as well; a request with a valid token reaches the action, and `User.Identity.IsAuthenticated` is true with the claims from the JWT payload available on `User.Claims`. From the controller's perspective, the only difference from cookie authentication is the credential transport — the authorization model on top is identical.

## Lifetimes and refresh tokens

Bearer tokens are designed to be short-lived because possession alone authenticates. A common production lifetime is between five minutes and one hour. Anything longer enlarges the window during which a leaked token is usable; anything much shorter forces the client to re-authenticate so often that user experience suffers.

Short lifetimes do not mean the user re-enters their password every five minutes. The standard pattern pairs a short-lived *access token* (the JWT presented to APIs) with a longer-lived *refresh token* (a separate, opaque credential the client keeps and presents only to the issuer). When the access token expires, the client sends the refresh token to the issuer's token endpoint and receives a new access token in return. The refresh token never goes to APIs and can be revoked server-side by removing it from the issuer's database. The split lets access tokens stay self-contained and verifiable offline while keeping a server-side handle on the long-lived credential.

## When JWT is the wrong choice

The validate-without-asking property that makes JWTs efficient is also their main limitation. A cookie session can be killed by deleting one row from a database; the next request the client makes finds no session and is rejected. A JWT, once issued, is valid until it expires — there is no central place where the validating service asks "is this token still good?" Revocation strategies for JWT exist, but each one gives back something the format was designed to avoid:

- A revocation list (block-list of token IDs) requires every API to query a central store on every request, recreating the session lookup the format was meant to eliminate.
- Rotating signing keys invalidates every token signed with the old key, not just the ones for the compromised user.
- Shortening the lifetime to seconds approximates revocation by attrition but pushes load to the issuer's token endpoint.

Where revocation is critical — administrative bans, password resets that must take effect now, compliance scenarios that demand immediate session termination — server-side sessions or opaque tokens with central introspection are a better fit than JWTs. Where revocation can wait until the natural expiry of a short-lived token, JWTs offer scale and decentralisation that session stores cannot match. The decision is about how long an unwanted credential may remain valid, not about which technology is more sophisticated.

## Summary

Bearer tokens move authentication out of the cookie jar and into the `Authorization` header, fitting the calling models cookies do not — mobile apps, single-page applications, and service-to-service calls. The format that fills the slot is the JWT: a base64-encoded header, payload, and signature whose payload carries standard claims (`iss`, `sub`, `aud`, `exp`, `nbf`, `iat`) and whose signature lets the recipient verify the token without contacting the issuer. ASP.NET Core's `JwtBearer` middleware, configured with an `Authority` and an `Audience`, handles the full validation path; controllers opt in with `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`. Short access-token lifetimes paired with longer-lived refresh tokens keep the leak window small without forcing constant re-authentication. The trade-off is revocation: a JWT is valid until it expires, so scenarios that demand immediate session termination remain better served by server-side sessions than by tokens.
