+++
title = "OAuth 2.0 and OpenID Connect"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 70
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/5-identity-and-security/7-oauth-and-oidc.html)

[Se presentationen på svenska](/presentations/course-book/5-identity-and-security/7-oauth-and-oidc-swe.html)

---

A web application that needs to call a user's GitHub repositories, read their Google Calendar, or merely offer "Sign in with Microsoft" runs into the same problem: the application must act on behalf of the user without ever holding the user's password. Asking users to type their Google password into a third-party login form is both unsafe (the form could keylog the credential) and impractical (the third party has no good way to handle multi-factor prompts, account recovery, or revocation). OAuth 2.0 and OpenID Connect solve this problem by having a trusted third party — the authorization server — handle the credential and hand back a scoped token instead. The actor model, the browser-application flow, and the practical ASP.NET Core registration that wires Microsoft Entra ID into a webapp as the identity provider all build on that single shift in trust.

## The delegated-access problem

A traditional login form asks the user to enter a password directly into the application that is checking it. That model works when the application owns the user database and the credential. It breaks the moment the application wants to *use* a service that belongs to a different security domain — a calendar API hosted by Google, an issue tracker hosted by GitHub, a directory hosted by Microsoft Entra ID. The application cannot ask for a Google password and store it; Google would not honor it for API calls anyway, since the API expects its own tokens. Even reusing a password for a single sign-in event leaks the credential to a third party that has no legitimate need to see it.

The same problem appears in a smaller form for plain login. An application that wants to offer "Sign in with Google" without managing passwords itself needs Google to perform the credential check, then somehow tell the application "yes, this browser belongs to anna@example.com." The application never sees the password; it only needs a trustworthy assertion of identity.

**OAuth 2.0** is the delegated-authorization framework that lets a user grant a client application access to their resources on a third-party service without sharing the user's password with the client; the client receives an access token from an authorization server and presents it to the resource server. **OpenID Connect** (OIDC) is an authentication layer on top of OAuth 2.0; in addition to an access token, the authorization server returns an *ID token* (a JWT) that asserts who the user is, allowing the client to use the third-party service as an identity provider. The two protocols are routinely deployed together, but they answer different questions. OAuth answers "may this client call that API on the user's behalf?" — an authorization question. OIDC answers "who is the user logging in?" — an authentication question. The [authentication-versus-authorization split](/course-book/5-identity-and-security/1-authentication-vs-authorization/) introduced earlier in the Part applies here across organizational boundaries.

## The four actors

Both protocols define the same cast. Naming them precisely makes the message flow easier to follow.

The *resource owner* is the human user whose data or identity is being accessed. In a "Sign in with Google" flow, the resource owner is the person sitting at the browser. In a calendar-integration flow, the resource owner is the person whose calendar events are being read.

The *client* is the application requesting access. The OAuth specification uses "client" for any application that calls the protocol — a server-side webapp, a single-page application, a mobile app, a CLI tool. The client is identified by a `client_id` issued by the authorization server when the application is registered, and (for confidential clients) authenticated by a `client_secret`.

An **authorization server** is the OAuth/OIDC component that authenticates the user, asks for consent, and issues access (and ID) tokens to clients; Microsoft Entra ID, Google, GitHub, and Auth0 are common authorization servers. The authorization server owns the user directory, runs the login UI, validates passwords or multi-factor prompts, and is the only party in the system that ever sees the credential.

The *resource server* hosts the API the client wants to call. It accepts access tokens issued by the authorization server, validates them, and serves protected data. Microsoft Graph is a resource server; the GitHub REST API is a resource server. For pure sign-in scenarios there is often no separate resource server — the client only needs the ID token to know who the user is.

The four actors run on different machines under different administrative control, which is precisely why a token-based protocol is needed: trust between them must be carried by signed tokens rather than shared databases.

## Grant types at a high level

OAuth defines several *grant types* — message flows the client uses to obtain a token. Each grant type targets a different combination of client capability and trust. Four are commonly encountered.

The *authorization code* grant is the standard flow for interactive user logins. The client redirects the user's browser to the authorization server, the user authenticates there, and the authorization server redirects back to the client with a short-lived authorization code that the client exchanges for tokens. This is the only grant type covered in detail below, because it is the one a webapp uses for "Sign in with Microsoft."

The *client credentials* grant is for machine-to-machine calls where there is no user. A backend service authenticates with its own `client_id` and `client_secret` and receives an access token representing itself. A daily reporting job that queries Microsoft Graph for tenant-wide statistics uses this grant; no user is sitting at a browser.

The *device code* grant handles input-constrained devices — a smart TV, a CLI on a remote server — that cannot host a browser comfortably. The device displays a short code; the user opens a browser on a phone or laptop, enters the code, and authenticates there. The device polls the authorization server until the login completes. The Azure CLI's `az login` uses this grant when run in a remote shell.

The *refresh token* grant is not a stand-alone login flow; it lets a client trade a long-lived refresh token (issued alongside an access token in any of the above flows) for a new access token without re-prompting the user. Access tokens are deliberately short-lived (an hour is typical); refresh tokens carry the durability.

The original specification also defined an *implicit* grant for browser apps and a *resource owner password credentials* grant for legacy migrations. Both are now discouraged — implicit because it returns tokens directly in URL fragments where they leak through browser history, password credentials because it defeats the entire point of delegated access. Current guidance directs browser apps to authorization code with PKCE and treats password credentials as a stop-gap only.

## Authorization code with PKCE

Authorization code with *PKCE* (Proof Key for Code Exchange, pronounced "pixie") is the recommended grant for any client running in a browser or on a user's device — public clients that cannot keep a `client_secret` truly secret. The flow runs in two halves separated by a browser redirect.

In the first half, the client generates a random `code_verifier` (a high-entropy string), hashes it into a `code_challenge`, and redirects the browser to the authorization server's authorization endpoint with parameters that include the `client_id`, the `redirect_uri` the response should come back to, the requested `scope`, a `state` value, and the `code_challenge`. The authorization server presents its login UI, validates the user's credentials, possibly shows a consent screen listing the requested scopes, and redirects the browser back to the registered `redirect_uri` with a one-time `code` and the original `state`.

In the second half, the client makes a back-channel HTTPS call to the token endpoint, posting the `code`, the `redirect_uri` (for cross-checking), and the original `code_verifier`. The authorization server hashes the verifier, compares it to the stored challenge, and — if they match — issues an access token, optionally a refresh token, and (if OIDC was requested) an ID token. The first half runs through the user's browser and is observable to anyone who can intercept the redirect; the second half runs server-to-server and carries the actual tokens.

PKCE matters because a malicious app on the same device cannot redeem an intercepted authorization code — the attacker would need the original `code_verifier`, which never left the legitimate client. The flow turns the public client into something that can authenticate the second half of the exchange without holding a long-lived secret.

### Redirect URIs and the state parameter

The `redirect_uri` is the location the authorization server sends the browser back to. Authorization servers require the URI to be pre-registered for the `client_id` and reject any request that does not match exactly. The check is strict — `https://app.example.com/auth/callback` and `https://app.example.com/auth/callback/` (trailing slash) are different URIs. This pre-registration prevents an attacker from substituting their own callback to harvest authorization codes intended for a legitimate app.

The `state` parameter is a random value the client generates per login attempt, sends in the outbound redirect, and validates on the inbound callback. If the value does not match what the client stored at the start of the flow, the callback did not originate from a login the client initiated and the request is rejected. The mechanism prevents cross-site request forgery against the callback endpoint: an attacker who tricks the user's browser into hitting the callback with a code chosen by the attacker cannot supply a `state` that matches a value the client is expecting.

## Scopes — what the token may do

A *scope* is a string the client requests, and the authorization server includes in the issued token, that names a category of access. Scopes are the unit of consent: the consent screen the user sees lists scopes ("Read your email," "Edit your calendar"), and the resulting access token carries only those scopes the user agreed to.

For OIDC sign-in, the standard scopes are `openid` (required to trigger OIDC and receive an ID token), `profile` (request name, picture, basic profile claims), and `email` (request the user's email address). For API access, scope strings are defined by the resource server: Microsoft Graph uses scopes like `User.Read` and `Mail.Send`; GitHub uses `repo` and `read:user`. A client that asks for the smallest set of scopes that suffices for its function follows the principle of least privilege; users are also less likely to abandon a consent screen that requests three scopes than one that requests thirty.

## What OIDC adds — the ID token

OAuth 2.0 by itself returns an access token that lets the client call an API. It does not, by design, tell the client *who* logged in. An access token is a permission slip, not an identity assertion; the resource server validates it but the client treats it as opaque.

OpenID Connect adds an ID token alongside the access token. The ID token is a JWT (the same structure introduced in [Bearer tokens and JWT](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/)) signed by the authorization server. Its payload carries claims that name the user: `sub` (a stable subject identifier within the issuer), `name`, `email`, plus the issuer and audience claims that pin the token to a specific authorization server and a specific client. Because the token is signed, the client can verify the claims without calling the authorization server again — the signature, validated against the authorization server's public key, proves the issuer minted exactly these claims.

The ID token is for the *client* to consume, not for forwarding to a resource server. A common mistake is to send the ID token in `Authorization: Bearer` headers to APIs; the API is expecting an access token with appropriate scopes, and ID tokens have a different audience. Keep the two separate.

## Authentication versus authorization, restated

OAuth and OIDC together cover both halves of the question Part V opened with. OAuth handles authorization across organizational boundaries — "may this client, acting for this user, call that API?" OIDC handles authentication across the same boundary — "who is this user?" The application code that follows looks much like the cookie-authentication code from earlier chapters: the framework hands controllers a `ClaimsPrincipal` populated from the ID token's claims, and `[Authorize]` attributes work unchanged. The difference is where the identity came from. Instead of a local user database with hashed passwords, the identity came from a remote authorization server that already knows the user.

The same OIDC machinery is used in deployment pipelines for a different purpose: GitHub Actions can present an OIDC token to Azure to obtain temporary deployment credentials, replacing long-lived service-principal secrets. That flavor of OIDC is workload identity federation rather than user authentication and is covered in the DevOps & Delivery part of this book; the protocol is the same, but the resource owner is a workflow rather than a person.

## Worked example — registering OIDC with Microsoft Entra ID

ASP.NET Core ships first-class support for OIDC. The `AddOpenIdConnect` extension method registers the handler, and the framework takes care of the redirects, code exchange, token validation, and `ClaimsPrincipal` construction. The application code that follows is the production-shape registration for signing users in against Microsoft Entra ID.

Figure 1: OIDC registration in `Program.cs`

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.ClientId = builder.Configuration["Entra:ClientId"];
        options.ClientSecret = builder.Configuration["Entra:ClientSecret"];
        options.ResponseType = "code";
        options.UsePkce = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
    });
```

Two schemes are registered together. The cookie scheme is the default — once OIDC has signed the user in, the framework writes a local cookie so subsequent requests do not have to round-trip back to Entra. The OIDC scheme is the *challenge* scheme: when an unauthenticated request hits a controller marked `[Authorize]`, the framework redirects the browser to Entra, and the cookie scheme picks up afterwards. The `Authority` is the Entra tenant's OIDC discovery base URL; the framework fetches `Authority + "/.well-known/openid-configuration"` at startup to discover the authorization, token, and JWKS endpoints, so the code never hard-codes them. `ResponseType = "code"` and `UsePkce = true` select authorization code with PKCE. The three scopes named line up with the OIDC standard set discussed above. `SaveTokens` keeps the access token in the auth properties so downstream code can call APIs as the signed-in user.

The companion exercise track [Authentication and Authorization](/exercises/10-webapp-development/4-authentication-authorization/) develops the cookie half of this picture from the bottom up; layering OIDC on top reuses the same `ClaimsPrincipal` model the exercises build, with Entra ID rather than a hardcoded user list as the identity source.

## Summary

OAuth 2.0 lets a client application call an API on behalf of a user without ever seeing the user's password — the user authenticates with an authorization server, which issues a scoped access token the client can present to a resource server. OpenID Connect adds an ID token, a signed JWT that asserts who the user is, turning the same authorization server into an identity provider. Authorization code with PKCE is the recommended grant for browser-based clients; the redirect URI is pre-registered with the authorization server, the `state` parameter prevents callback CSRF, and PKCE binds the back-channel code exchange to the original front-channel request. Scopes name the units of access the user consents to; a client asks for the minimum it needs. ASP.NET Core's `AddOpenIdConnect` registers the handler, and the framework takes the response and produces the same `ClaimsPrincipal` that local cookie authentication would have produced — so authorization rules written against claims do not change when the identity source moves from a local database to Microsoft Entra ID.
