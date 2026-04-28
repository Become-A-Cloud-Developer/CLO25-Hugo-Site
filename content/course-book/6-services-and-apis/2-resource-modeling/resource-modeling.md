+++
title = "Resource Modeling and URIs"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 20
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/6-services-and-apis/2-resource-modeling.html)

[Se presentationen på svenska](/presentations/course-book/6-services-and-apis/2-resource-modeling-swe.html)

---

The hardest part of designing an HTTP API is not writing the controller code; it is deciding which things the API exposes and what they are called. Once a URI is published, clients depend on it, so any rename or restructure breaks them. Resource modeling is the practice of choosing those names deliberately — picking the nouns the API addresses, deciding which of them are lists and which are single items, and laying them out under URIs that other developers can guess without reading the docs.

## The resource as the unit of API design

A REST API does not expose functions; it exposes [resources](/course-book/6-services-and-apis/1-rest-principles/). The first design decision for any new endpoint is therefore not "what verb describes this operation?" but "what noun does this operation act on?" If the answer is a noun the API already addresses, the new operation is another HTTP method on the existing URI. If the answer is a noun the API has not modelled yet, a new URI is needed. This shift from verb-thinking to noun-thinking is what gives REST APIs their shape.

The two URI shapes that carry the bulk of an API are the collection and the item. A **resource collection** is a URI that addresses a list of resources of the same type (`/quotes`, `/orders`); it is conventionally manipulated with `GET` to list, `POST` to create, and `DELETE` only when removing the entire collection makes sense. A **resource item** is a URI that addresses one specific resource by its identifier (`/quotes/42`, `/orders/abc-123`); manipulated with `GET` to retrieve, `PUT` or `PATCH` to modify, and `DELETE` to remove. Almost every endpoint in a typical API falls into one of these two shapes.

The discipline is what makes the API predictable. A developer who has called `GET /api/customers` and `GET /api/customers/{id}` can guess that creation is `POST /api/customers` and deletion is `DELETE /api/customers/{id}` without consulting the documentation. That guessability is not an accident — it is the payoff of modeling the API as resources rather than as a list of named operations.

## Naming conventions

Resource URIs use **plural nouns** for collections. `/quotes`, `/orders`, `/customers` — never `/quote`, `/order`, or `/customer`. The plural reads correctly in both directions: `GET /quotes` returns the list, and `GET /quotes/42` reads as "quote forty-two from the quotes collection." Mixing singular and plural across an API forces clients to memorise which is which, and the inconsistency surfaces immediately the first time someone autocompletes a URL.

Verbs do not appear in URIs. `GET /api/quotes` lists quotes; there is no `/api/getQuotes`, `/api/listQuotes`, or `/api/quotes/list`. The HTTP method is the verb. A URI that contains a verb either duplicates the method (`/getQuotes` plus `GET`) or contradicts it (`POST /api/deleteQuote/42`), and neither helps clients. The only legitimate exception is operations that genuinely do not fit a CRUD shape — `POST /api/quotes/42/publish` for a state transition that has no obvious resource of its own — and even there the noun-first preference (`POST /api/quotes/42/publications`) usually reads better.

URIs are case-sensitive in the spec but conventionally lowercase. Words are joined with hyphens, never underscores or camelCase: `/customer-orders`, not `/customerOrders` or `/customer_orders`. The hyphen choice matches how URLs render in browser address bars and search engines and avoids the shift-key cost of underscores. The path segments stay short — `/quotes` beats `/quote-records`, and `/customers/{id}/orders` beats `/customer-management/customers/{customer-id}/customer-orders`.

### Identifiers in item URIs

The identifier in a resource-item URI is whatever the server uses internally to address that resource. Integer ids (`/quotes/42`) are common when the database is the source of truth and the id is auto-generated. GUIDs (`/orders/3f2a8c91-...`) are used when ids are generated client-side or when sequential ids leak business information (an order id of `1024` tells a competitor roughly how many orders the business has processed). Slugs (`/articles/getting-started-with-rest`) are used when the URI doubles as something humans read or share. The choice trades off readability, leak resistance, and uniqueness guarantees; what matters for resource modeling is that whatever identifier is chosen becomes part of the URI contract and cannot change.

## Collection and item URIs

Each of the two URI shapes has its own conventional set of HTTP methods. The combination of shape and method is what gives a REST API its grid of operations.

| URI shape | Method | Conventional meaning |
|-----------|--------|----------------------|
| `/quotes` | `GET` | List the collection |
| `/quotes` | `POST` | Create a new item; `Location` header points at the new item |
| `/quotes` | `DELETE` | Remove the entire collection (rare) |
| `/quotes/{id}` | `GET` | Retrieve one item |
| `/quotes/{id}` | `PUT` | Replace the item |
| `/quotes/{id}` | `PATCH` | Modify part of the item |
| `/quotes/{id}` | `DELETE` | Remove the item |

`PUT` on an item URI replaces the resource — the request body must contain every field the resource has. `PATCH` modifies part of it — the request body contains only the fields that change. `PUT` is the simpler default; `PATCH` matters once a resource has enough fields that always sending the whole thing is wasteful or risks overwriting concurrent changes.

`POST` on a collection URI is the conventional create operation. The 201 response carries a `Location` header pointing at the newly-created item URI, so the client receives the canonical address of the resource it just created. ASP.NET Core's `CreatedAtAction` helper builds this header from a named action; the framework computes the URL, so a route refactor does not break clients (the [REST controllers exercise](/exercises/4-services-and-apis/1-rest-api-and-dtos/) demonstrates the pattern).

What does *not* go on a collection URI is per-item operations. `DELETE /quotes` does not delete one quote — it deletes the whole list. `PUT /quotes` does not update one quote — it replaces the entire collection. Both are valid HTTP, but they almost always indicate a design mistake; the intended operation belongs on the item URI instead.

## Hierarchical relationships

When one resource belongs to another, the URI hierarchy can express the relationship: `/customers/{id}/orders` addresses the orders that belong to a specific customer. The customer is the parent; the orders are sub-resources scoped under that customer. The hierarchy reads as a path: "customer 42's orders," "customer 42's order number 7." A client listing `GET /customers/42/orders` cannot accidentally see customer 99's orders, because the URI itself excludes them.

Hierarchy is a design choice, not a database mirror. A relational database has a `customer_id` foreign key on the `orders` table; that does not mean the API has to expose the relationship as a sub-resource. Two URI shapes are available for the same data:

- **Sub-resource:** `GET /customers/42/orders` — list orders belonging to customer 42.
- **Flat with filter:** `GET /orders?customerId=42` — list all orders, filtered by customer.

Both work; both can return identical JSON. The difference is what the URI emphasises. The sub-resource shape says "orders are something a customer has." The flat shape says "orders are an independent thing that happen to reference a customer." The right choice depends on whether the parent owns the child conceptually and on how clients want to reach the data.

### When to use which

| Decision factor | Sub-resource (`/customers/{id}/orders`) | Flat with filter (`?customerId=42`) |
|-----------------|-----------------------------------------|--------------------------------------|
| Child cannot exist without parent | Yes — orders only make sense per customer | Less natural |
| Most queries are scoped to one parent | Yes — the URI carries the scope | Adequate, but the filter is mandatory |
| Cross-parent queries are common | Awkward — needs a separate flat route | Yes — `?status=pending` across all customers |
| Authorization scope matches parent | Yes — easy to enforce "this caller can only see their own customer's orders" | Possible, but the check is per query |
| The child has a global identity | Less natural — `/customers/42/orders/7` plus `/orders/7` is duplication | Yes — `/orders/7` is canonical |

Some APIs expose both — `/customers/42/orders` for scoped listing and creation, `/orders/{id}` for direct access by global id. That is fine when a clear convention separates the two: the sub-resource is the *list* under a parent, and the flat item URI is the *canonical* address of one resource. Mixing them inconsistently — sometimes scoped, sometimes flat, with no rule — is what creates confusion.

## Filtering, pagination, and sub-resources at a glance

Query parameters carry options that modify how a collection is read but do not change which resource it addresses. Filtering is the most common use: `GET /api/quotes?status=published&author=knuth` returns the subset of the collection that matches. The collection URI is still `/api/quotes` — the filter narrows the response, it does not address a different resource. This separation matters because `/quotes?status=published` and `/quotes?status=draft` are two views of one resource, not two distinct resources.

Sorting (`?sort=createdAt`), field selection (`?fields=id,author`), and search (`?q=optimization`) follow the same rule. They are query parameters, not path segments, because they describe how to project the collection, not which collection to address.

Pagination is the same idea applied to size: a request like `GET /api/quotes?page=3&size=20` returns one slice of a long list. The strategies (offset, page, cursor) and the response-envelope shape are detailed in [pagination, idempotency, and rate limiting](/course-book/6-services-and-apis/6-pagination-and-rate-limiting/). What matters for resource modeling is that pagination is a query-parameter concern on the collection URI, not a separate resource — `/quotes/page/3` is a URI mistake.

Sub-resources extend the path. The conventional shape `/quotes/{id}/comments` reads as "the comments belonging to quote 42." `GET` lists them, `POST` creates one, and the resulting comment is addressable at `/quotes/{id}/comments/{commentId}` or, if comments have global ids, at `/comments/{id}`. The hierarchy depth tracks the conceptual ownership; URIs deeper than three levels (`/customers/42/orders/7/items/3/discounts`) become unwieldy and usually indicate that the deeper resources deserve their own top-level URI.

## URI stability — the contract

A published URI is a contract. Once a client has called `GET /api/quotes/42`, that URI is part of the system the client depends on. Renaming it to `/api/cool-quotes/42` does not "improve" the API — it breaks every client that wired the old URI into its code, configuration, bookmarks, or external integrations. Resource modeling is therefore not a thing that happens once and then evolves freely; the model the API ships is the model it lives with for as long as the URI exists.

The remedy when a model genuinely needs to change is API versioning — the `/v1/quotes` path stays exactly as it was, while a parallel `/v2/quotes` introduces the new shape. Old clients keep working against `v1`; new clients adopt `v2`. The version number is the explicit contract boundary that says "everything under here will not be silently restructured." Versioning has its own trade-offs and is detailed in [status codes, versioning, and error responses](/course-book/6-services-and-apis/4-status-codes-and-errors/); what matters for resource modeling is that versioning is the alternative to changing a URI in place, and changing a URI in place is what the contract forbids.

This is why the early naming decisions matter so much. A plural-vs-singular slip-up, a verb left in a URI, or a relationship modeled as a sub-resource that should have been flat (or vice versa) — each of these is cheap to fix on day one and expensive to fix on day three hundred. The ten minutes spent at the whiteboard deciding `/quotes` vs `/quote` saves the breaking change six months later.

## Worked example — the `CloudCiApi` quote endpoints

The companion exercise scaffolds an ASP.NET Core Web API called `CloudCiApi` whose only resource is the `Quote`. The three endpoints it exposes are exactly the conventional shapes for a collection plus an item:

```text
GET    /api/quotes          → 200, JSON array of QuoteDto
GET    /api/quotes/{id}     → 200 with the QuoteDto, or 404 if no such id
POST   /api/quotes          → 201 Created, Location: /api/Quotes/5, body = the new QuoteDto
```

The collection URI is `/api/quotes` — plural, lowercase, no verb, no version segment. `GET` lists; `POST` creates. The item URI is `/api/quotes/{id}` — same plural noun followed by the integer identifier the in-memory store assigns. `GET` retrieves; the controller returns `404` when the id does not match a stored quote. The `[HttpGet("{id:int}")]` route constraint pins the segment to integers so `/api/quotes/abc` does not even reach the action — the framework returns `404` from routing.

The `POST` response is the place where resource modeling and HTTP semantics meet. The status is `201 Created`; the `Location` header points at the new item URI; the body carries the freshly-created `QuoteDto`. The controller never hard-codes the URL — `CreatedAtAction(nameof(GetById), new { id = quote.Id }, dto)` asks the framework to build it from the named action. If the route template ever changed, the framework would still produce the correct `Location` value, and clients reading the header would still find the resource at the right address. The naming convention (one plural noun, one HTTP method per shape) and the framework feature (route generation by action name) reinforce each other — both are aimed at keeping the URI stable as the implementation evolves.

What the exercise does *not* yet have is a `PUT`, `PATCH`, or `DELETE`. Those would each go on `/api/quotes/{id}` if added, and each would slot into the grid above without changing any of the three URIs already published. The next exercise in the chapter adds an API-key gate; it does not change the resource model — only the conditions under which clients are allowed to hit it. That is the resource model paying off: a security change is a middleware change, not a URL change.

## Summary

A REST API exposes resources, not functions, and the URIs that name those resources are the API's primary contract. Collections are addressed by plural nouns (`/quotes`); items are addressed by the same plural noun plus an identifier (`/quotes/42`). Each shape has a conventional set of HTTP methods — `GET`/`POST` on collections, `GET`/`PUT`/`PATCH`/`DELETE` on items — and matching that grid is what makes an API guessable. Hierarchical relationships can be expressed either as sub-resources (`/customers/{id}/orders`) or as flat resources with a foreign-key filter (`/orders?customerId=42`); the choice depends on ownership semantics and query patterns, not on the database schema. Filtering and pagination ride on the collection URI as query parameters, never as new path segments. URIs are case-sensitive, lowercase, hyphenated, verb-free, and — once published — stable. When the model genuinely needs to change, the right tool is a new API version, not a renamed URI. The `CloudCiApi` quote endpoints in the companion exercise are the smallest concrete instance of these rules: one collection URI, one item URI, three methods, no verbs, no surprises.
