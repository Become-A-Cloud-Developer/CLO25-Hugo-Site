+++
title = "REST Principles"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 10
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/6-services-and-apis/1-rest-principles.html)

[Se presentationen på svenska](/presentations/course-book/6-services-and-apis/1-rest-principles-swe.html)

---

An HTTP API is a contract between two programs that have never met. The client was written in a different language, runs on a different machine, and may have shipped before the server it is now talking to existed. For that conversation to work, both sides need a shared vocabulary for what URLs mean, which methods do what, and what a response is allowed to look like. Without a shared style, every API would invent its own conventions and every client would need bespoke code per server. **REST** is the dominant style HTTP APIs follow to avoid that fate, and a small set of design constraints is what makes a service worth calling RESTful.

## What REST is and why it exists

**REST** (Representational State Transfer) is an architectural style for HTTP APIs in which the server exposes named resources, the client manipulates representations of those resources using the standard HTTP methods, and each request carries everything the server needs to process it. The style was articulated by Roy Fielding in his 2000 dissertation as a description of how the web itself was already working — what made HTTP scale to millions of servers and billions of clients was a particular set of architectural constraints that any networked system could adopt.

REST is not a protocol. There is no `REST/1.0` specification to validate against, no schema language baked into the style, no lint that a service either passes or fails. REST is a set of constraints on top of [HTTP](/course-book/3-application-development/1-http-fundamentals/), and a service is more or less RESTful depending on how many of those constraints it honours. That looseness is what makes REST useful — teams can adopt the parts that pay off for their use case and skip the parts that do not — but it is also what makes "is this REST?" a fuzzy question in code review.

The constraints exist for one reason: they make HTTP services that scale, evolve, and interoperate. A service that follows them can put a cache in front of it, swap its storage layer behind it, add new endpoints without breaking old clients, and let any HTTP-aware tool (a browser, `curl`, a CDN, a load balancer) participate in the conversation without special knowledge of the service.

## The architectural constraints

Fielding's REST has six constraints. Five of them are load-bearing for an HTTP API; the sixth (code-on-demand) is optional and rarely applied. The five that matter are client-server separation, statelessness, cacheability, the uniform interface, and a layered system. Each constrains the service in a way that buys back a property the network needs.

### Client-server separation

The client and the server are independent programs that communicate only through the API. The client knows nothing about how the server stores data, which database it uses, or how it scales. The server knows nothing about how the client renders the response, which framework it uses, or whether the user is on a phone or a terminal. The only contract between them is the set of resources, methods, and representations the API exposes.

This separation is what lets the two halves evolve independently. A team can rewrite the server in a different language, swap the database, or move from one cloud region to another, and as long as the API contract holds, every client keeps working. The same API can serve a web frontend, a mobile app, a CLI, and a batch job — each is a different client of the same server, and none of them knows or cares about the others.

### Statelessness

**Statelessness** is the REST constraint that each request from a client must contain all the information the server needs to process it; the server keeps no client session between requests, which makes the API trivially horizontally scalable but pushes session state into the client or into a separate token. The server is allowed to keep persistent state — quotes in a database, blobs in storage — but it must not keep *per-conversation* state in memory between calls.

The payoff is operational. Any replica of the service can serve any request, because no replica has special knowledge about a particular client. A load balancer can round-robin requests across ten instances; if one instance falls over, the next request lands on another and works. Nothing has to migrate. Nothing has to be sticky.

The cost is that authentication context, cursor positions, and any other "where was this client" state has to ride along on every request. In practice that means an `Authorization` header on every call, identifiers in every URL, and pagination cursors echoed back to the server each time. The client gets bigger; the server gets simpler; the system scales.

### Cacheability

Responses must declare whether they can be cached, and for how long. The server signals cacheability with HTTP headers — `Cache-Control`, `ETag`, `Last-Modified` — and any cache that sits between the client and the server (the browser, a CDN, a reverse proxy) is free to honour those signals without involving the application.

A cacheable `GET /quotes/42` response can be served from a CDN edge node in a few milliseconds without the request ever reaching the application, which is how a small backend can serve millions of clients. The constraint is that the application has to be honest about what is cacheable: a response that varies per user must say so (`Cache-Control: private` or `Vary: Authorization`), or every user will end up seeing the first user's data.

### Uniform interface

The uniform interface is the constraint that makes REST recognizable. It has four sub-rules: resources are identified by URIs, those resources are manipulated through representations, messages are self-describing, and the application's state is driven by hypermedia (the much-debated HATEOAS rule). In practice, this means an API exposes a set of nouns at predictable URLs, and clients act on those nouns using the standard HTTP methods.

A **resource** is anything an API addresses with a URI — a customer record, a list of orders, a single image — and that the API can return a representation of, accept updates to, or delete. The resource is the *concept*; the representation is the *bytes on the wire* — usually JSON, sometimes XML, sometimes a binary format. The same resource can have multiple representations: `/quotes/42` could return JSON to one client and XML to another based on the `Accept` header.

The standard [HTTP methods](/course-book/3-application-development/1-http-fundamentals/) carry conventional meaning across every REST API:

- `GET` retrieves a representation of a resource without changing it
- `POST` creates a new resource (typically against a collection URL)
- `PUT` replaces a resource with the supplied representation
- `PATCH` updates part of a resource
- `DELETE` removes a resource

The conventions matter because they let intermediaries reason about traffic without understanding the application. A cache knows `GET` is safe to store. A retry library knows `PUT` is idempotent and safe to retry, while `POST` is not. A monitoring tool can count `5xx` responses without knowing what the service does.

The fourth sub-rule, hypermedia as the engine of application state (HATEOAS), is the idea that a response should embed the URLs the client can follow next, so the client navigates the API the way a browser navigates the web — clicking links rather than knowing URL patterns in advance. Few production APIs implement HATEOAS strictly, because clients almost always know the URL patterns from documentation rather than discovering them at runtime. The trade-off is real: HATEOAS makes URL refactoring safer at the cost of every response carrying link metadata the client may not use.

### Layered system

A REST client cannot tell whether it is talking to the application server directly or to a reverse proxy, a load balancer, or a CDN that forwards to the application server. Each layer can add its own behaviour — caching, authentication, rate limiting, TLS termination — without the client knowing. The layered constraint is what lets a service insert Azure Front Door, an API Management gateway, or a Container Apps ingress in front of an application, and the application's clients keep working unchanged.

## A worked example: GET /quotes/42

The companion exercise builds a tiny ASP.NET Core API called `CloudCiApi` whose `Quotes` resource exhibits every constraint above. A `GET` request for a single quote looks like this on the wire:

```text
GET /api/quotes/42 HTTP/1.1
Host: cloudci-api.example.com
Accept: application/json
```

And the response:

```text
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: public, max-age=60

{
  "id": 42,
  "author": "Edsger W. Dijkstra",
  "text": "Simplicity is prerequisite for reliability.",
  "createdAt": "2025-09-14T10:23:11Z"
}
```

Every constraint is visible in those few lines. The URL `/api/quotes/42` names a resource — quote forty-two — and the JSON body is one representation of it. The method `GET` is the standard verb for retrieval, so any cache or proxy in the path knows this response can be stored. The `Cache-Control` header makes that explicit: any layer is welcome to keep this response for sixty seconds. The request carries no session cookie and the server kept no record of the client between calls — this same request would work identically against any replica of the service, which is what statelessness buys. And the client did not need to know whether the request hit the application directly or went through Front Door, the Container Apps ingress, and a Kestrel listener — the layered system is invisible to it.

The companion exercise [REST API and DTOs](/exercises/4-services-and-apis/1-rest-api-and-dtos/) builds the controller that produces this response, alongside the entity-versus-DTO split that keeps the wire shape independent of the in-memory class.

## The Richardson Maturity Model

The Richardson Maturity Model is a four-level scale, due to Leonard Richardson, that grades how thoroughly an API embraces the REST constraints. It is not a certification — it is a way to talk about which constraints a given service honours and which it skips.

| Level | What the API does | Typical example |
|-------|-------------------|-----------------|
| 0 | One URL, one method (`POST /api`), action encoded in the body | XML-RPC, SOAP-over-HTTP |
| 1 | Multiple URLs (resources), but still typically `POST` for everything | Many early "REST" APIs |
| 2 | Resources plus the standard HTTP methods and status codes | Most production APIs called "REST" today |
| 3 | Level 2 plus HATEOAS — responses embed links to next actions | GitHub API, PayPal API |

Most APIs that the industry calls "REST" sit firmly at Level 2: they expose resources, use `GET`/`POST`/`PUT`/`DELETE` for the obvious operations, and return meaningful HTTP [status codes](/course-book/3-application-development/1-http-fundamentals/) like `200`, `201`, `404`, and `409`. Level 3 is the strict-REST aspiration; it is rare in the wild because the cost of producing and consuming hypermedia rarely beats the cost of documenting URL patterns.

For a working API, Level 2 is usually the sweet spot. Resources, standard methods, and meaningful status codes give clients enough predictability that any HTTP-aware tool participates correctly, without forcing every team to adopt link-driven navigation.

## When REST is not the right choice

REST is the default style for an HTTP API because it composes well with everything else on the web — caches, proxies, load balancers, browser developer tools, OpenAPI tooling. It is not the only choice, and for some workloads it is genuinely the wrong one.

*gRPC* uses HTTP/2 and Protocol Buffers rather than JSON over HTTP/1.1. It produces smaller payloads and supports streaming in both directions, which makes it a better fit for service-to-service traffic inside a backend where latency and throughput matter and the clients are programs, not browsers. The trade-off is that gRPC traffic is opaque to the HTTP toolchain — a cache cannot inspect it, a browser cannot make it directly without a translation layer, and `curl` cannot be used to debug it.

*GraphQL* lets the client specify exactly which fields it wants from a single endpoint, which is a better fit when one screen needs data from many resources and the client wants to avoid a fan-out of REST calls. The trade-off is that caching becomes much harder (every query is potentially unique), the server has to defend against expensive queries, and the simplicity of `GET /quotes/42` is gone.

For an HTTP API that public clients consume, that has predictable resources, and that benefits from CDN caching, REST is almost always the right starting point. For high-throughput service-to-service traffic or for client-driven query shapes, the alternatives are worth considering, alongside the REST API that fronts the system from the outside.

## Summary

REST is an architectural style for HTTP APIs that constrains how clients and servers communicate, with the goal of producing services that scale, cache well, and evolve without breaking clients. The five load-bearing constraints — client-server separation, statelessness, cacheability, uniform interface, and a layered system — each trade a degree of freedom for an operational property that the network needs. Most production APIs implement REST loosely, sitting at Richardson Level 2 with resources, standard HTTP methods, and meaningful status codes, but skipping the strict HATEOAS requirement of Level 3. REST is not the only style — gRPC and GraphQL solve problems REST does not — but for an HTTP API consumed by external clients, REST remains the default, and understanding its constraints is what lets a service team know which conventions to honour, which to bend, and what each compromise costs.
