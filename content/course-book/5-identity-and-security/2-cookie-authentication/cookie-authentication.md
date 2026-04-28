+++
title = "Cookie-Based Authentication and Sessions"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
weight = 20
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/5-identity-and-security/2-cookie-authentication.html)

[Se presentationen på svenska](/presentations/course-book/5-identity-and-security/2-cookie-authentication-swe.html)

---

HTTP carries no memory between requests. Every request a browser sends to a web application arrives as if no previous request ever happened — the protocol itself does not record who is on the other end. A web application that lets a user sign in once and stay signed in across pages must therefore add its own mechanism for recognising the same browser across requests. Cookie-based authentication is the oldest and still the most common answer to that requirement, and the mechanism every other browser-based authentication scheme on ASP.NET Core ultimately builds on.

The mechanism worth tracing end to end runs from the browser storing and replaying cookies, through the server turning a verified password into an encrypted authentication ticket, the sign-in and sign-out handshakes, the cookie attributes that harden the scheme against attack, and on to why cross-site request forgery (CSRF) is a problem only cookie-authenticated sites face. The pattern shown here is the foundation that [ASP.NET Core Identity](/course-book/5-identity-and-security/3-aspnet-core-identity/) layers user persistence and password hashing on top of, and the contrast against which bearer tokens are introduced in [Bearer Tokens and JWT](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/).

## How browsers handle cookies

A **cookie** is a small piece of data the server asks the browser to store and return on every subsequent request to the same domain; an **authentication cookie** carries an encrypted record of who the browser is signed in as, allowing the server to recognize the user without re-authenticating. The browser is the cookie store — the server only sees cookies it has previously asked the browser to remember.

The mechanism is defined by two HTTP headers. When the server wants to set a cookie, it returns a `Set-Cookie` response header naming the cookie, its value, and any attributes that govern lifetime and scope. The browser parses that header, stores the cookie in its cookie jar against the issuing domain, and on every later request to that domain attaches the cookie automatically in a `Cookie` request header. Application code never has to decide to send the cookie — the browser sends it on every same-origin request, including image and script loads, until the cookie expires or is deleted.

For authentication, three properties of this behaviour matter:

- **The browser sends the cookie on every request to the domain**, including requests triggered by other sites (a `<form>` on `evil.example` that posts to `bank.example`). This is convenient for the legitimate user and the root cause of CSRF.
- **The cookie is bound to the domain that issued it**, not to the user account. If two users share a browser profile, they share the cookie jar; sign-out must therefore explicitly invalidate the cookie rather than merely forgetting it on the server.
- **The cookie's value is opaque to the browser**. The browser stores and replays bytes; it does not parse them. The server is free to put whatever encoded data in the cookie as long as the resulting size stays within the few-kilobyte budget browsers accept.

## The authentication ticket inside the cookie

The temptation when first implementing cookie authentication is to store the user's name in the cookie and trust it on the way back. That is unsafe: the cookie sits on a machine the server does not control, and a hostile user can edit the value before sending it. ASP.NET Core's cookie-authentication middleware solves this by treating the cookie as the transport for an **authentication ticket** that is both encrypted and integrity-protected before it leaves the server.

The ticket is the serialised form of a `ClaimsPrincipal` (the [principal](/course-book/5-identity-and-security/1-authentication-vs-authorization/) representing the signed-in user) together with metadata such as the issue time, expiration, and the authentication scheme that produced it. Before being placed in the `Set-Cookie` header, the ticket is run through the framework's data-protection API:

1. The serialised bytes are encrypted with a symmetric key held only on the server, so the cookie's contents are unreadable by the client and by anyone who steals it from the browser without also stealing the server's keys.
2. The ciphertext is signed with a message-authentication code, so any modification to the cookie value causes verification to fail when the cookie returns. A user who edits a single byte gets logged out, not promoted.
3. The result is base64-encoded so it survives the HTTP header transport.

The cookie is therefore opaque to the client and tamper-evident to the server. On every incoming request, the cookie middleware reads the cookie, verifies the signature, decrypts the ticket, reconstructs the `ClaimsPrincipal`, and assigns it to `HttpContext.User`. From the [controller's](/course-book/3-application-development/3-the-mvc-pattern/) perspective the user is signed in; the cookie machinery is invisible.

## The sign-in handshake

Sign-in is the one-time exchange that converts a password into a cookie. The shape of the handshake is the same in every web framework; ASP.NET Core's specific names are shown for concreteness.

The user requests a protected page, the framework recognises that no authentication cookie is present, and it issues a redirect to a login route. That route renders a form whose action posts a username and password back to the server over HTTPS. A controller action receives the form data, looks up the user record, and verifies the supplied password against the stored hash. If verification fails, the action re-renders the form with an error and no cookie is issued. If verification succeeds, the action constructs a `ClaimsPrincipal` describing the user and calls `HttpContext.SignInAsync`, which is the framework's instruction to the cookie middleware to emit a `Set-Cookie` response header carrying the encrypted ticket. The action then redirects the browser back to the originally requested page. On that follow-up request the browser attaches the new cookie, the middleware decrypts it, and the page renders for the now-authenticated user.

```csharp
public class AccountController : Controller
{
    private readonly IUserDirectory _users;

    public AccountController(IUserDirectory users) => _users = users;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid) return View(model);

        var user = _users.FindByName(model.Username);
        if (user is null || !user.VerifyPassword(model.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
            },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return LocalRedirect(returnUrl ?? "/");
    }
}
```

Two details in the action carry weight. The `ClaimsIdentity` is constructed with the cookie scheme name as its authentication type — this is the marker the middleware looks for when reconstructing the principal on later requests. The redirect uses `LocalRedirect` rather than plain `Redirect`, which refuses absolute URLs; this prevents an attacker from crafting a `?returnUrl=https://evil.example` link that bounces the freshly signed-in user (and any reflected query data) to an external site.

For the cookie middleware to know what scheme to look for and what cookie name to set, the application registers cookie authentication once at startup:

```csharp
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Forbidden";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "CloudSoft.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

`AddAuthentication` declares the default scheme name; `AddCookie` registers the middleware that handles that scheme. The options block records every choice the application makes about the cookie's lifetime and security attributes. Order matters in the pipeline: `UseAuthentication` must run before `UseAuthorization`, because the principal must be on `HttpContext.User` before any `[Authorize]` attribute is evaluated.

## The sign-out handshake

Sign-out is symmetric. The user posts to a sign-out endpoint, the action calls `HttpContext.SignOutAsync` for the cookie scheme, and the middleware emits a `Set-Cookie` header that overwrites the existing cookie with an empty value and a past expiration date. The browser sees that the cookie has expired and removes it from the cookie jar. The next request arrives without an authentication cookie, the middleware leaves `HttpContext.User` unauthenticated, and any `[Authorize]`-protected page bounces back to the login route.

Because the cookie is self-contained — the server keeps no per-cookie session record by default — sign-out cannot revoke a cookie that has already been copied off the user's machine. This is the trade-off of stateless cookies: revocation requires either a server-side allow-list, short cookie lifetimes, or a key roll that invalidates every issued cookie at once. For most web applications the lifetime knob is sufficient.

## Cookie attributes that matter for security

A cookie's attributes appear in the `Set-Cookie` response header alongside its value. Four attributes carry the security weight of the scheme.

| Attribute | Purpose | Failure mode if omitted |
|-----------|---------|-------------------------|
| `HttpOnly` | Hides the cookie from JavaScript running in the page | A cross-site scripting bug exfiltrates the cookie via `document.cookie` |
| `Secure` | Sends the cookie only over HTTPS | A network attacker on plain HTTP captures the cookie verbatim |
| `SameSite=Lax` (or `Strict`) | Suppresses the cookie on most cross-site requests | A cross-site form post replays the user's session against the application |
| `Expires` / `Max-Age` | Bounds the cookie's lifetime in the browser | A stolen cookie remains valid indefinitely |

`HttpOnly` and `Secure` are unconditional: an authentication cookie that is readable by JavaScript or sent over HTTP is broken regardless of the rest of the application's care. `SameSite` is the browser-level defence against CSRF and is covered in its own section below. The `Expires` attribute determines the cookie's persistence in the browser; if omitted the cookie becomes a *session cookie* that the browser deletes when it closes (the term refers to the browser session, not the application session).

### Session lifetime versus sliding expiration

The cookie's expiration time is the application's primary lever for balancing usability against the consequences of a stolen cookie. Two strategies dominate.

A *fixed expiration* issues a cookie that is valid for a defined duration from the moment of sign-in — eight hours, for example — regardless of activity. The cookie goes away at the same wall-clock time whether the user worked continuously or stepped away for lunch. The simplicity is appealing, but active users get logged out mid-task.

A *sliding expiration* extends the cookie's lifetime on each request, capped at the configured maximum. ASP.NET Core implements this by re-emitting the cookie when more than half of `ExpireTimeSpan` has elapsed since the last issue. A user who works steadily through a six-hour day stays signed in; a cookie that is then stolen and unused for the full window expires before the attacker can exploit it. The cost is more `Set-Cookie` headers on the wire and a longer effective lifetime for active sessions, which makes the rotation question (how often the underlying authentication ticket is refreshed against the user store) more pressing.

A **session** is the period during which a particular client is recognized as the same authenticated user across multiple requests; in cookie-based authentication the session ends when the cookie expires, the user signs out, or the server invalidates the session record. Sliding expiration extends the session as the user works; sign-out terminates it explicitly; a key rotation on the server invalidates every outstanding session at once.

## Why CSRF is a cookie-specific problem

Cross-site request forgery exploits the very property that makes cookies convenient: the browser attaches the authentication cookie to *every* request to the application's domain, including requests originating from other tabs and other sites. A hostile site can host a hidden form that posts to `https://bank.example/transfer` with attacker-chosen field values, and as long as the user is signed in to the bank in another tab, the browser cheerfully attaches the bank's cookie to the cross-site post. The bank sees a fully authenticated request and acts on it.

Bearer-token APIs are not exposed to this attack because tokens are not attached automatically — application code has to read the token from storage and place it in an `Authorization` header, and that code is bound by the same-origin policy. CSRF is therefore a problem peculiar to cookie-authenticated sites, and the defence has to live at the application layer.

The browser-level defence is the `SameSite` cookie attribute. With `SameSite=Lax` (the current default in evergreen browsers) the browser withholds the cookie from cross-site `POST`, `PUT`, `DELETE`, and `PATCH` requests, while still attaching it to top-level `GET` navigations so links into the application keep working. `SameSite=Strict` withholds the cookie from every cross-site request including top-level navigations, which is safer but breaks deep-linked sign-ins from email and chat. `Lax` is the right default for most applications.

The application-level defence is the **anti-forgery token** — a per-session secret embedded in HTML forms and validated on POST; it prevents a malicious site from causing the user's browser to submit a form to your site using the user's authentication cookie. ASP.NET Core implements it as a pair: a long-lived cookie carrying half of the secret, and a hidden form field carrying the other half. The hostile site can cause the browser to send the cookie half (the browser attaches cookies automatically), but it cannot read the cookie or the hidden field across origins, so it cannot construct a matching form-field half. When the server's `[ValidateAntiForgeryToken]` attribute compares the two halves on POST and finds they do not match, the request is rejected with `400 Bad Request` before any controller code runs. The token is bound to the user's session, regenerated on sign-in, and invalidated on sign-out, which closes the loop with the cookie scheme described above.

The combination of `SameSite=Lax` and `[ValidateAntiForgeryToken]` is belt-and-braces: the browser refuses to send the cookie on most cross-site posts, and the server refuses to act on a post that arrives without a matching token even if a future browser bug or unusual configuration delivers the cookie anyway.

## Putting the pieces together

The companion exercise series at [`/exercises/10-webapp-development/4-authentication-authorization/`](/exercises/10-webapp-development/4-authentication-authorization/) walks the entire mechanism in code: a hardcoded user list and a login form post in the first exercise, role-aware claims in the second, claims-based policies and the antiforgery middleware in the third. The same `Set-Cookie` flow described here drives every step; subsequent chapters of this Part replace pieces of it (the user store, the credential format, the third-party identity provider) without disturbing the cookie machinery itself.

## Summary

Cookie-based authentication uses an HTTP cookie to carry an encrypted, signed authentication ticket between the browser and the server, so a user who signs in once is recognised on every later request without re-supplying a password. The sign-in handshake validates a credential, builds a `ClaimsPrincipal`, and calls `HttpContext.SignInAsync` to emit the cookie; sign-out emits an expired cookie that the browser then drops. The cookie's `HttpOnly`, `Secure`, `SameSite`, and expiration attributes are what keep the scheme safe against script-based theft, network-based theft, cross-site replay, and indefinite reuse. Sliding expiration extends an active session up to a configured maximum while letting idle sessions die. Cross-site request forgery is the structural cost of having the browser attach the cookie to every request, and the anti-forgery token plus `SameSite=Lax` is the standard defence. Every later chapter of this Part — Identity, claims and policies, bearer tokens, and external sign-in — either builds on this cookie scheme or contrasts itself with it.
