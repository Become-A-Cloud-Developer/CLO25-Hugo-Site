+++
title = "Authentication vs Authorization"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
weight = 10
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/5-identity-and-security/1-authentication-vs-authorization.html)

[Se presentationen på svenska](/presentations/course-book/5-identity-and-security/1-authentication-vs-authorization-swe.html)

---

Web applications that serve more than one user must answer two questions on every secured request: who is making this call, and is that caller allowed to do what they are asking to do? The two questions sound similar, but they are answered by different mechanisms, at different points in the request pipeline, against different sources of truth. Conflating them produces fragile code that either trusts the wrong caller or denies the right one. The conceptual split between the two questions, the framework primitives that answer each one, and the credential formats and decision rules built on that foundation are the through-line of Part V.

## The two questions every secured request must answer

**Authentication** is the process of confirming who is making a request — verifying that a presented credential (password, token, certificate) maps to a known identity in the system. The output is an identity: a name, a user ID, sometimes a set of attached attributes such as email or tenant. Authentication does not decide whether the caller is allowed to do anything; it only establishes who they are.

**Authorization** is the process of deciding whether an authenticated identity is allowed to perform a requested action — a separate decision from authentication, and one that depends on the identity's roles, claims, or policy match. The input is an already-known identity and the resource being requested; the output is a yes-or-no answer to "may this caller proceed?"

The distinction matters because the two questions consult different sources of truth and fail in different ways. Authentication failure means the credential did not match — the response is `401 Unauthorized` and the standard remedy is "log in again." Authorization failure means the credential did match but the resulting identity lacks the necessary permission — the response is `403 Forbidden` and logging in again will not change the outcome. A system that returns the same status code for both, or treats both as the same kind of error, leaks information and confuses callers.

### Why frameworks keep them separate

ASP.NET Core implements authentication and authorization as two distinct stages because the inputs, the trust model, and the rate of change are different. Authentication consults a credential store (a user database, a token issuer, a certificate authority) and tends to change rarely — adding a second authentication scheme is a deployment-level decision. Authorization consults the application's business rules — which roles can edit which resources, which claims gate which actions — and changes whenever the product changes. Separating the two means a feature team can adjust authorization rules without touching credential validation, and a platform team can add a new authentication scheme (cookies in a browser, JWT in an API) without rewriting controllers.

The separation also enforces a useful discipline: authorization code never sees a raw credential. By the time a controller asks "is this user an admin?", the framework has already validated whatever the caller presented, attached an in-memory identity to the request, and discarded the password or token. Business code reasons about identity, not credentials.

## The principal — the identity attached to a request

Once authentication succeeds, the framework needs a place to put the validated identity so the rest of the pipeline can read it. That place is the principal.

A **principal** is the abstract identity attached to an authenticated request; in ASP.NET Core, the `ClaimsPrincipal` exposes the identity's name and claims to controller code via `HttpContext.User`. The `ClaimsPrincipal` is constructed by the authentication handler — the cookie handler, the JWT bearer handler, the certificate handler, or any other registered scheme — and is the single object that authorization code reads from.

The principal carries a name (usually the username or user ID), one or more identities (in advanced multi-scheme scenarios), and a list of claims. A claim is a name-value pair such as `("role", "Admin")` or `("email", "anna@example.com")`. The cookie handler reads the encrypted authentication ticket and reconstructs a `ClaimsPrincipal` from it. The JWT handler parses the token's payload and projects each payload field as a claim. The credential format on the wire differs; the in-memory shape the rest of the application sees is the same.

This uniformity is the reason that swapping cookies for JWT — or supporting both at once — does not require rewriting controllers. A controller that reads `User.Identity.Name` or asks `User.IsInRole("Admin")` is talking to the principal, not to the cookie or the token. The transport changed; the contract did not.

## Where each decision happens in the pipeline

ASP.NET Core processes a request as an ordered chain of middleware components, each of which can short-circuit the request or pass it to the next. Authentication and authorization sit at fixed positions in that chain.

The authentication middleware runs early. When `app.UseAuthentication()` is registered, every incoming request passes through it before reaching the routing or endpoint stages. The middleware inspects the request for a credential — a cookie, an `Authorization` header, a client certificate — and hands it to the configured authentication handler. If the credential validates, the handler builds a `ClaimsPrincipal` and assigns it to `HttpContext.User`. If no credential is present, `HttpContext.User` is set to an unauthenticated principal (the `IsAuthenticated` property is `false`). Crucially, the middleware does not reject the request when no credential is present — it merely records the absence. Public endpoints must remain reachable.

The authorization filter runs later, once routing has matched the request to a controller action. The framework checks for an `[Authorize]` attribute on the action or controller, and if one is present, it evaluates the requirement against `HttpContext.User`. Bare `[Authorize]` requires only that the user be authenticated. `[Authorize(Roles = "Admin")]` additionally requires a role claim with that value. `[Authorize(Policy = "ManagerOnly")]` evaluates a named policy registered at startup. If the requirement fails, the framework short-circuits the response with `401 Unauthorized` (when the user is not authenticated at all) or `403 Forbidden` (when they are authenticated but lack the required claim). If it succeeds, control proceeds to the action method.

The ordering is what makes the split work: by the time the authorization filter runs, `HttpContext.User` is already populated. Authorization never validates credentials and never queries the user database; it reads claims off the principal that authentication already produced.

The basic form of the `[Authorize]` attribute is part of the MVC framework — see [the MVC pattern](/course-book/3-application-development/3-the-mvc-pattern/) for the controller and routing context. The remaining sections describe what the attribute does once a request reaches it.

## Credentials authentication accepts

Authentication is a family of mechanisms, not a single procedure. The handler that runs depends on the registered scheme and on what the request actually carries. Each handler accepts a different credential format but produces the same `ClaimsPrincipal` shape on success.

A *password* is the credential a human user types into a login form. The server compares it to a stored hash, and on a match issues a session credential — typically an authentication cookie — so the user is not asked to retype the password on every request. Passwords are presented once per session; the cookie carries the established identity afterward.

A [bearer token](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/) is a credential a client presents in the `Authorization: Bearer <token>` header on every API request. JSON Web Tokens are the most common bearer-token format: the token itself contains the claims, signed by the issuer, so the recipient can validate the token without contacting a session store. APIs prefer tokens over cookies because they are easier to issue, easier to revoke (by short lifetimes), and not subject to the cross-site cookie rules browsers enforce.

A *client certificate* is a credential presented during the TLS handshake itself, before the HTTP request body is read. The certificate's subject and issuer are validated against a trust chain, and the resulting identity is projected into a `ClaimsPrincipal`. Certificates suit machine-to-machine and high-assurance scenarios where deploying a private key to the client is acceptable.

Other forms exist — API keys for simpler service-to-service calls, OAuth tokens issued by external authorization servers, federated assertions from enterprise identity providers — and each is implemented as an authentication handler. The principle is consistent: the handler validates a credential and produces a principal.

## Decisions authorization makes

Once a principal exists, authorization decides whether the principal may proceed. ASP.NET Core supports three styles of decision, each more flexible than the last.

A *role check* asks whether the principal belongs to a named group: `[Authorize(Roles = "Admin")]` or `User.IsInRole("Reviewer")`. Roles are coarse-grained — an identity either has the role or it does not — and they suit broad organizational categories such as administrator, reviewer, or candidate. The framework treats role membership as a special claim under the hood, but the syntax is shorter for the common case.

A *claim check* asks whether the principal carries a specific claim. A controller might require `User.HasClaim("tenant", currentTenant)` before allowing access to tenant-specific data. Claim checks are finer-grained than role checks and naturally encode multi-dimensional facts about the identity (tenant, department, security clearance, license tier).

A *policy evaluation* runs a named policy registered at startup against the principal. A policy bundles one or more requirements — "must be authenticated" plus "must have role Manager" plus "must have a `region` claim matching the request" — into a single named rule that controllers reference with `[Authorize(Policy = "ManagerInRegion")]`. Policies centralize business logic so that the rule lives in one place and every controller that opts in stays consistent. The full mechanics of roles, claims, and policies are developed in [Roles, claims, and policies](/course-book/5-identity-and-security/4-roles-claims-and-policies/).

## A worked example

Exercise 4 in the application-development track walks through the full arc on a hardcoded user list. The relevant fragment is a controller that renders a "Who Am I?" page only to authenticated callers.

```csharp
[Authorize]
public class WhoAmIController : Controller
{
    public IActionResult Index()
    {
        var name = User.Identity?.Name ?? "anonymous";
        var role = User.FindFirst("role")?.Value ?? "(no role)";
        return View(new WhoAmIViewModel(name, role));
    }
}
```

When a request hits this controller, the framework runs the authentication middleware first. The middleware inspects the request for a cookie matching the configured cookie scheme; if one is present and validates, the cookie handler decrypts the authentication ticket and assigns the resulting `ClaimsPrincipal` to `HttpContext.User`. The authorization filter then sees the `[Authorize]` attribute, asks the principal `IsAuthenticated`, and either lets the request reach the action or short-circuits to the login page. By the time `Index()` runs, `User` is guaranteed to be an authenticated principal, and the action can read the name and role claims directly. No password validation happens in this controller; no user-database query happens here either. The controller reasons about identity, not credentials.

The companion exercise [Authentication and Authorization](/exercises/10-webapp-development/4-authentication-authorization/) develops this scenario through four steps: hardcoded credentials and a login form, attaching role claims, inspecting the full `ClaimsPrincipal`, and adding CSRF protection. Each step layers on the same framework primitives this chapter introduced.

## How the rest of Part V fits together

The remaining chapters drill into specific axes of the authentication-and-authorization split. [Cookie-based authentication and sessions](/course-book/5-identity-and-security/2-cookie-authentication/) covers the credential format used in browser scenarios — how the authentication cookie is issued, validated, and protected from cross-site request forgery. [ASP.NET Core Identity](/course-book/5-identity-and-security/3-aspnet-core-identity/) replaces the hardcoded user list with a managed user store, password hashing, and registration flows. [Roles, claims, and policies](/course-book/5-identity-and-security/4-roles-claims-and-policies/) develops the authorization side: how `ClaimsPrincipal` represents identity, how role checks and policy evaluation work, and when to choose one over the other. [Bearer tokens and JWT](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/) replaces the cookie with a token format suited to APIs and explains how the same `ClaimsPrincipal` is reconstructed from a signed payload. [API keys and machine-to-machine](/course-book/5-identity-and-security/6-api-keys/) and [OAuth 2.0 and OpenID Connect](/course-book/5-identity-and-security/7-oauth-and-oidc/) cover service-to-service and federated-identity scenarios. [Secret management](/course-book/5-identity-and-security/8-secret-management/) closes the Part with the operational side: how the cryptographic keys, connection strings, and signing secrets that all the previous chapters depend on are stored and rotated safely.

Each subsequent chapter reuses the conceptual frame this chapter establishes — the split between authentication and authorization, the principal as the in-memory identity, the middleware-then-filter pipeline order — and adds the specific mechanism that fills in one part of the picture.

## Summary

Every secured request answers two questions: who is the caller, and may the caller proceed? Authentication answers the first by validating a credential — a password, a bearer token, a certificate — and producing a `ClaimsPrincipal` attached to `HttpContext.User`. Authorization answers the second by reading roles, claims, and policy results off that principal, never touching the credential itself. ASP.NET Core enforces the split by running authentication middleware early in the pipeline and the authorization filter later, after routing. Keeping the two stages separate lets credential formats change without rewriting business rules, and lets business rules change without revisiting credential validation. The chapters that follow develop each axis in turn — cookies, Identity, claims, JWT, API keys, OAuth, and secret management — on the conceptual foundation this chapter laid down.
