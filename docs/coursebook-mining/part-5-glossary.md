# Part V — Glossary

Terminology contract for the eight chapters of Part V — Identity & Security.

## Terms owned by this Part

### Authentication
- **Owner chapter**: `1-authentication-vs-authorization`
- **Canonical definition**: **Authentication** is the process of confirming who is making a request — verifying that a presented credential (password, token, certificate) maps to a known identity in the system.
- **Used by chapters**: 1 (owner), 2, 3, 5, 6, 7

### Authorization
- **Owner chapter**: `1-authentication-vs-authorization`
- **Canonical definition**: **Authorization** is the process of deciding whether an authenticated identity is allowed to perform a requested action — a separate decision from authentication, and one that depends on the identity's roles, claims, or policy match.
- **Used by chapters**: 1 (owner), 4, 5, 6

### Principal
- **Owner chapter**: `1-authentication-vs-authorization`
- **Canonical definition**: A **principal** is the abstract identity attached to an authenticated request; in ASP.NET Core, the `ClaimsPrincipal` exposes the identity's name and claims to controller code via `HttpContext.User`.
- **Used by chapters**: 1 (owner), 2, 4

### Cookie (authentication)
- **Owner chapter**: `2-cookie-authentication`
- **Canonical definition**: A **cookie** is a small piece of data the server asks the browser to store and return on every subsequent request to the same domain; an **authentication cookie** carries an encrypted record of who the browser is signed in as, allowing the server to recognize the user without re-authenticating.
- **Used by chapters**: 2 (owner), 3, 7

### Session
- **Owner chapter**: `2-cookie-authentication`
- **Canonical definition**: A **session** is the period during which a particular client is recognized as the same authenticated user across multiple requests; in cookie-based authentication the session ends when the cookie expires, the user signs out, or the server invalidates the session record.
- **Used by chapters**: 2 (owner), 3

### Anti-forgery token
- **Owner chapter**: `2-cookie-authentication`
- **Canonical definition**: An **anti-forgery token** (also called a CSRF token) is a per-session secret embedded in HTML forms and validated on POST; it prevents a malicious site from causing the user's browser to submit a form to your site using the user's authentication cookie.
- **Used by chapters**: 2 (owner)

### ASP.NET Core Identity
- **Owner chapter**: `3-aspnet-core-identity`
- **Canonical definition**: **ASP.NET Core Identity** is the framework's user-management system: it stores user records (with hashed passwords), provides `UserManager<TUser>` and `SignInManager<TUser>` services, and integrates with cookie authentication to handle registration, sign-in, password reset, and lockout.
- **Used by chapters**: 3 (owner), 4

### User store
- **Owner chapter**: `3-aspnet-core-identity`
- **Canonical definition**: A **user store** is the persistence backend ASP.NET Core Identity uses to read and write user records; common implementations include in-memory (development only), Entity Framework Core against SQL, and custom implementations targeting other databases.
- **Used by chapters**: 3 (owner)

### Password hashing
- **Owner chapter**: `3-aspnet-core-identity`
- **Canonical definition**: **Password hashing** transforms a plaintext password into a fixed-length value through a slow, salted one-way function (PBKDF2, bcrypt, Argon2), making it computationally infeasible to recover the original password from the hash; the user store keeps only the hash.
- **Used by chapters**: 3 (owner)

### Claim
- **Owner chapter**: `4-roles-claims-and-policies`
- **Canonical definition**: A **claim** is a name-value pair carried by a `ClaimsPrincipal` that asserts something about the identity — name, email, role membership, tenant, scope; authorization decisions read claims off the principal rather than re-querying the user database.
- **Used by chapters**: 4 (owner), 5, 7

### Role
- **Owner chapter**: `4-roles-claims-and-policies`
- **Canonical definition**: A **role** is a named group that an identity belongs to (Admin, Candidate, Reviewer); the framework treats role membership as a special claim and provides shorthand syntax such as `[Authorize(Roles = "Admin")]` to require it.
- **Used by chapters**: 4 (owner), 3

### Policy (authorization)
- **Owner chapter**: `4-roles-claims-and-policies`
- **Canonical definition**: An **authorization policy** is a named set of requirements registered at startup; controllers and actions opt in with `[Authorize(Policy = "Name")]`, and the framework evaluates each requirement against the principal's claims to decide allow or deny.
- **Used by chapters**: 4 (owner), 5

### Bearer token
- **Owner chapter**: `5-bearer-tokens-and-jwt`
- **Canonical definition**: A **bearer token** is an opaque or structured credential a client sends in an HTTP request's `Authorization: Bearer <token>` header; whoever bears the token is treated as authenticated, so bearer tokens must be transmitted over TLS and kept short-lived.
- **Used by chapters**: 5 (owner), 7

### JWT
- **Owner chapter**: `5-bearer-tokens-and-jwt`
- **Canonical definition**: A **JWT** (JSON Web Token) is a structured bearer-token format consisting of a base64-encoded header, payload, and signature joined by dots; the payload carries claims (subject, issuer, audience, expiration), and the signature lets the recipient verify the token has not been tampered with.
- **Used by chapters**: 5 (owner), 6, 7

### Issuer / audience
- **Owner chapter**: `5-bearer-tokens-and-jwt`
- **Canonical definition**: A JWT's **issuer** (`iss` claim) names the service that signed the token, and the **audience** (`aud` claim) names the service that should accept it; the validating service rejects tokens whose issuer or audience do not match its configured expectations, preventing tokens minted for one service from being replayed against another.
- **Used by chapters**: 5 (owner), 7

### API key
- **Owner chapter**: `6-api-keys`
- **Canonical definition**: An **API key** is a long, opaque shared secret a client presents in a request header (commonly `X-Api-Key`) to identify itself to the server; unlike a JWT it carries no claims, and unlike a user password it identifies a calling application rather than a person.
- **Used by chapters**: 6 (owner)

### Key rotation
- **Owner chapter**: `6-api-keys`
- **Canonical definition**: **Key rotation** is the practice of issuing a new credential and retiring the old one on a schedule (or after a suspected compromise); rotation hardens systems against long-lived stolen credentials and is supported by API gateways and secret stores that allow multiple valid keys during a transition window.
- **Used by chapters**: 6 (owner), 8

### OAuth 2.0
- **Owner chapter**: `7-oauth-and-oidc`
- **Canonical definition**: **OAuth 2.0** is the delegated-authorization framework that lets a user grant a client application access to their resources on a third-party service without sharing the user's password with the client; the client receives an access token from an authorization server and presents it to the resource server.
- **Used by chapters**: 7 (owner)

### OpenID Connect
- **Owner chapter**: `7-oauth-and-oidc`
- **Canonical definition**: **OpenID Connect** (OIDC) is an authentication layer on top of OAuth 2.0; in addition to an access token, the authorization server returns an **ID token** (a JWT) that asserts who the user is, allowing the client to use the third-party service as an identity provider.
- **Used by chapters**: 7 (owner)

### Authorization server
- **Owner chapter**: `7-oauth-and-oidc`
- **Canonical definition**: An **authorization server** is the OAuth/OIDC component that authenticates the user, asks for consent, and issues access (and ID) tokens to clients; Microsoft Entra ID, Google, GitHub, and Auth0 are common authorization servers.
- **Used by chapters**: 7 (owner)

### Azure Key Vault
- **Owner chapter**: `8-secret-management`
- **Canonical definition**: **Azure Key Vault** is a managed service that stores secrets, encryption keys, and certificates with audit logging and access control; applications retrieve secrets at runtime over HTTPS using a token, never embedding secrets in code or configuration files.
- **Used by chapters**: 8 (owner)

### Managed identity
- **Owner chapter**: `8-secret-management`
- **Canonical definition**: A **managed identity** is an Entra ID identity that Azure attaches to a compute resource (VM, App Service, Container App, GitHub Actions runner via federation) and rotates automatically; the application authenticates to other Azure services as that identity without storing any credentials.
- **Used by chapters**: 8 (owner)

### RBAC role assignment
- **Owner chapter**: `8-secret-management`
- **Canonical definition**: An **RBAC role assignment** in Azure pairs a security principal (user, group, service principal, managed identity) with a role definition (built-in or custom) at a specific scope (subscription, resource group, individual resource); the assignment grants exactly the actions the role allows on resources in that scope.
- **Used by chapters**: 8 (owner)

## Terms borrowed from earlier Parts

### HTTP / Request / Response / Header / Method
- **Defined in**: Part III — Application Development / `1-http-fundamentals`
- **Reference link**: `/course-book/3-application-development/1-http-fundamentals/`

### Controller / View / [Authorize] attribute (basic form)
- **Defined in**: Part III — Application Development / `3-the-mvc-pattern`
- **Reference link**: `/course-book/3-application-development/3-the-mvc-pattern/`

### IConfiguration / appsettings.json / Environment variable / User-secrets
- **Defined in**: Part III — Application Development / `5-configuration-and-environments`
- **Reference link**: `/course-book/3-application-development/5-configuration-and-environments/`

### Dependency injection / IServiceCollection / Lifetime (Singleton, Scoped)
- **Defined in**: Part III — Application Development / `6-dependency-injection`
- **Reference link**: `/course-book/3-application-development/6-dependency-injection/`

### Connection string (when discussing storing connection strings as secrets)
- **Defined in**: Part IV — Data Access / `3-connections-and-transactions`
- **Reference link**: `/course-book/4-data-access/3-connections-and-transactions/`

### Firewall (when discussing network-level access controls vs identity-level)
- **Defined in**: Part II — Infrastructure / Network / `4-firewalls`
- **Reference link**: `/course-book/2-infrastructure/network/4-firewalls/`
