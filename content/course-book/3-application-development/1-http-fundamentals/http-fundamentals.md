+++
title = "HTTP Fundamentals"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 10
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/3-application-development/1-http-fundamentals.html)

[Se presentationen på svenska](/presentations/course-book/3-application-development/1-http-fundamentals-swe.html)

---

A web application is useless until something on the network can reach it and ask it to do work. Two unrelated programs — a browser written in C++ and a web server written in C# — must agree on the exact bytes that travel between them, the order those bytes arrive in, and the meaning of every field. That agreement is HTTP, the protocol that every chapter in Part III either uses directly or sits on top of. Understanding the request-response cycle, the methods, the status codes, and the URI structure makes the rest of the application stack legible: routing, model binding, controller actions, and authentication all become refinements of the basic shape introduced here.

## Why HTTP exists

HTTP sits on top of the [TCP/IP stack](/course-book/2-infrastructure/network/) covered in Part II and inherits the [client-server model](/course-book/2-infrastructure/compute/1-what-is-a-server/) from the same Part. TCP delivers an ordered, reliable stream of bytes between two endpoints, but it has no opinion about what those bytes mean. A browser requesting a page and a database client retrieving a row both speak TCP, yet they cannot understand each other. An application protocol layered on top of TCP fixes the format of messages so that two programs from different vendors can interoperate.

**HTTP** (Hypertext Transfer Protocol) is the request-response protocol that browsers and APIs use to exchange data over a TCP connection. A client sends an HTTP request specifying a method, URI, and optional body, and the server returns an HTTP response with a status code, headers, and optional body. Every page load, every form submission, every API call from a mobile app, every health probe from an Azure load balancer follows this same shape.

Three properties of HTTP shape the rest of this Part:

- **Text-based message format.** Requests and responses are human-readable strings, which makes debugging with `curl` and browser developer tools tractable.
- **Statelessness.** Each request is processed in isolation; the server keeps no memory of previous requests on the same connection. State that needs to persist (a logged-in user, a shopping cart) is reconstructed from headers, cookies, or tokens carried with each request.
- **Uniform interface.** Every interaction uses the same small vocabulary — methods, URIs, status codes, headers — so a generic component (a browser, a proxy, a CDN) can handle traffic for any application without knowing its internals.

These properties are why HTTP scales from a single laptop running `dotnet run` to global infrastructure handling millions of concurrent connections.

## The request-response cycle

A single HTTP exchange has four steps. The client opens a TCP connection to the server's host and port and writes a request message into that connection. The server reads the request, dispatches it to the right piece of application code, and writes a response message back. The client reads the response and acts on it — rendering HTML, parsing JSON, following a redirect, or showing an error.

An **HTTP request** is a message sent by a client to a server, consisting of a method (such as `GET` or `POST`), a target URI, headers describing the request, and an optional body carrying data. The first line names the method and the request target. Headers follow as `Name: Value` pairs, one per line. A blank line separates the headers from the body, which may be empty.

An **HTTP response** is the message a server returns after handling a request, consisting of a status code, headers describing the response, and an optional body carrying the requested representation. The first line of the response carries the protocol version, the numeric status code, and a short text reason phrase. Headers describe the body that follows — its content type, its length, its cache rules. The body itself contains the HTML, JSON, image bytes, or whatever representation the URI resolves to.

A minimal request and the response it triggers look like this:

```text
GET /Home/Index HTTP/1.1
Host: localhost:7240
Accept: text/html

HTTP/1.1 200 OK
Content-Type: text/html; charset=utf-8
Content-Length: 1832

<!DOCTYPE html><html>...
```

The exchange is short, textual, and self-describing. A network capture, a `curl -v` run, or the Network tab in browser developer tools all show the same fields. This transparency is a deliberate design choice: any tool that can read text can inspect HTTP traffic.

## HTTP methods

An **HTTP method** specifies the action a client wants to perform on the resource identified by the URI; the most common are `GET` (retrieve), `POST` (create or submit), `PUT` (replace), `PATCH` (partial update), and `DELETE` (remove). The method appears as the first token of the request and tells the server how to interpret the URI and the body.

Most browser traffic is `GET` and `POST`. Programmatic clients and APIs add `PUT`, `PATCH`, and `DELETE` to model the full lifecycle of a resource. The method is not a free-form label — servers, proxies, and caches all read it and behave differently based on its declared semantics.

### GET versus POST

The distinction between `GET` and `POST` is the question students encounter first, and it is the one most often answered imprecisely. The two methods differ in three ways that matter at every level of the stack.

**Where the data goes.** A `GET` request encodes its parameters in the URI as a query string — `/products?category=books&page=2`. A `POST` request carries data in the request body, separate from the URI. Body data is not visible in the URL bar, is not stored in browser history, and is not written to most server access logs.

**What the request promises.** `GET` is _safe_ and _idempotent_: the server should not change state in response to a `GET`, and repeating the same `GET` should yield the same outcome. `POST` is neither safe nor idempotent. A `POST` is expected to change server state — create a record, charge a card, send an email — and repeating it produces a new effect each time. Browsers and proxies rely on this contract: a `GET` is freely cacheable, prefetchable, and replayable, while a `POST` triggers a confirmation dialog when the user tries to refresh the page after submitting a form.

**How they interact with the URL.** A `GET` produces a bookmarkable, shareable link, because the full request (method plus URI) is encoded in the address bar. A `POST` cannot be bookmarked: the body is gone after the request completes. Form submissions that mutate data therefore use `POST`; search forms that filter a listing use `GET`.

The practical guidance follows directly from the contract. Use `GET` to fetch and display data. Use `POST` to submit data that creates, updates, or otherwise mutates state. Sending a credit card number as a query parameter on a `GET` request is wrong twice — the data ends up in browser history and proxy logs, and the action it triggers is not safely repeatable.

### Idempotency in practice

A method is _idempotent_ when sending the same request many times has the same effect on the server as sending it once. `GET`, `PUT`, and `DELETE` are idempotent; `POST` and `PATCH` are not. The distinction matters when a request fails midway. If a client retries an idempotent request after a network timeout, the server reaches the same state regardless of how many times the request actually arrived. Retrying a `POST` after a timeout, on the other hand, risks creating two records for the same logical action — the canonical reason for the "Confirm form resubmission" dialog and for techniques like idempotency keys in payment APIs.

## Status codes

Every response carries a three-digit **HTTP status code** that signals the outcome, with the first digit grouping outcomes into 1xx informational, 2xx success, 3xx redirection, 4xx client error, and 5xx server error. The status line gives the client enough information to decide what to do next without parsing the response body.

The codes that appear most often in an ASP.NET Core MVC application are listed below.

| Code | Meaning | Typical cause |
|------|---------|---------------|
| 200 OK | Request succeeded; body holds the representation | A `GET` returning a view or JSON payload |
| 201 Created | A resource was created; `Location` header points to it | A `POST` to a REST endpoint that inserted a row |
| 204 No Content | Success with no body | A `DELETE` or a `POST` that does not return data |
| 301 Moved Permanently | The resource lives at a new URI permanently | URL restructuring; SEO redirects |
| 302 Found | Temporary redirect | Post-Redirect-Get pattern after a successful form submission |
| 400 Bad Request | The request is malformed or fails validation | Invalid model state in a controller action |
| 401 Unauthorized | Authentication is required or has failed | A request missing a valid auth cookie or token |
| 403 Forbidden | The caller is authenticated but not allowed | Authorization policy denied the action |
| 404 Not Found | No resource exists at the URI | Route did not match any controller action |
| 500 Internal Server Error | The server raised an unhandled exception | Bug in application code; missing dependency |

Two patterns are worth noting. Status codes in the 4xx range place the fault on the client; codes in the 5xx range place it on the server. The difference matters for retry logic — retrying a `404` will keep failing, while retrying a `503` may succeed once the server recovers. And the framework, not the developer, picks the status code in many cases: ASP.NET Core MVC turns an unhandled exception into `500`, an unmatched route into `404`, and a `ValidationProblem` result into `400` automatically.

## URIs

A **URI** (Uniform Resource Identifier) is the string a client uses to address a resource on a server, typically composed of a scheme, host, path, and optional query string and fragment. The URI is the single piece of information that ties an HTTP request to a specific piece of application logic.

The full structure of a URI seen by an ASP.NET Core application looks like this:

```text
https://localhost:7240/Newsletter/Subscribe?source=footer#confirmation
\___/   \____________/\___________________/\____________/\___________/
scheme  authority      path                 query         fragment
```

The _scheme_ (`http` or `https`) selects the transport — plain TCP or TLS-wrapped TCP. The _authority_ combines the host name and port. The _path_ identifies the resource within the server and is the part the routing system dispatches on. The _query_ is a `?`-prefixed list of `key=value` pairs separated by `&`, used by `GET` requests to pass parameters. The _fragment_ after `#` is never sent to the server — it stays in the browser and is used for client-side anchors.

In an ASP.NET Core MVC application, the path conventionally maps to `/{controller}/{action}/{id?}`. A request for `/Home/Index` invokes the `Index` action on `HomeController`. Routing is the subject of [the MVC pattern chapter](/course-book/3-application-development/3-the-mvc-pattern/); here it is enough to know that the path of the URI determines which C# method runs.

## Headers

Headers are the metadata channel of an HTTP message. They appear as `Name: Value` pairs, one per line, between the start line and the body. The same header syntax serves both requests and responses, but most headers are used in only one direction.

Common request headers include `Host` (which virtual host on the server), `Accept` (which content types the client can handle), `Authorization` (credentials such as a bearer token), `Cookie` (session and auth cookies the browser previously stored), and `Content-Type` (what is in the body, when there is one). Common response headers include `Content-Type` (what the body contains), `Content-Length` (how many bytes), `Location` (where to redirect to), `Set-Cookie` (cookies the server wants the browser to store), and `Cache-Control` (rules for caches and the browser).

Headers carry the information that does not fit cleanly into the method or URI. Authentication, content negotiation, caching, compression, and CORS all happen through headers. The body is for the resource representation; headers are for everything about it.

## Worked example: curl hitting an MVC action

A default ASP.NET Core MVC project scaffolded with `dotnet new mvc -n CloudSoft` includes a `HomeController` whose `Index` action returns the welcome view. With the application running locally on port 7240, a `curl` command can drive the full request-response cycle without a browser.

```bash
curl -v https://localhost:7240/Home/Index
```

The `-v` flag prints the request and response in full. The annotated output shows every concept introduced above:

```text
> GET /Home/Index HTTP/1.1
> Host: localhost:7240
> User-Agent: curl/8.4.0
> Accept: */*
>
< HTTP/1.1 200 OK
< Content-Type: text/html; charset=utf-8
< Date: Tue, 28 Apr 2026 09:14:22 GMT
< Server: Kestrel
< Transfer-Encoding: chunked
<
<!DOCTYPE html>
<html lang="en">
...
```

The lines beginning with `>` are the request `curl` sent. The first line names the method (`GET`), the path (`/Home/Index`), and the protocol version. The `Host`, `User-Agent`, and `Accept` headers describe the client and what it is willing to receive. There is no body, which is expected for a `GET`.

The lines beginning with `<` are the response. The status line reports `200 OK`, signalling success. The `Content-Type` header tells `curl` the body is HTML, encoded as UTF-8. The `Server` header reveals that the request was handled by Kestrel, the cross-platform web server that ships with ASP.NET Core. The HTML body that follows is the rendered output of the MVC view at `Views/Home/Index.cshtml`.

Internally, ASP.NET Core matched the path `/Home/Index` to the `Index` method on `HomeController`, executed the method, rendered the associated view to HTML, and wrote the bytes back through the same TCP connection. Routing, model binding, and view rendering — the subjects of [the MVC pattern](/course-book/3-application-development/3-the-mvc-pattern/) — all happened inside that single response.

A `POST` example illustrates the contrast. Submitting a newsletter form looks like this:

```bash
curl -v -X POST https://localhost:7240/Newsletter/Subscribe \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "email=ada@example.com"
```

The request now has a body; the `Content-Type` header tells the server how to parse it; and the controller action is expected to mutate state — adding the address to the subscriber list. A successful submission typically returns a `302` redirect to a confirmation page rather than a `200` with a body, following the Post-Redirect-Get pattern that prevents accidental duplicate submissions on browser refresh.

The companion exercise [Presentation Layer](/exercises/10-webapp-development/1-presentation-layer/) builds these requests step by step against an ASP.NET Core MVC project, starting from the scaffolded `HomeController` and adding forms, validation, and styled views.

## Summary

HTTP is the request-response protocol layered on TCP that lets a client and a server exchange messages whose shape both sides understand. Every request names a method and a URI, carries headers, and may have a body; every response carries a status code, headers, and an optional body. `GET` is safe and idempotent and encodes parameters in the query string; `POST` mutates state and carries data in the body, which is why mutating actions never use `GET`. Status codes tell the client the outcome in one number, with the leading digit narrowing the cause to client or server. URIs combine scheme, authority, path, and query into the address that routing dispatches on. The rest of Part III — ASP.NET Core, the MVC pattern, three-tier architecture, configuration, and dependency injection — refines how a server turns those requests into responses.
