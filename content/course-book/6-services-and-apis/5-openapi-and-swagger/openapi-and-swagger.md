+++
title = "OpenAPI and Swagger"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 50
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/6-services-and-apis/5-openapi-and-swagger.html)

[Se presentationen på svenska](/presentations/course-book/6-services-and-apis/5-openapi-and-swagger-swe.html)

---

An HTTP API without machine-readable documentation forces every client integrator to read source code or guess. That guessing scales badly. Two integrators reach two different conclusions about what `POST /quotes` accepts; a third writes a client that works against the current contract and breaks the next time a property is renamed; a fourth gives up and emails the team for a Postman collection. The fix is a contract document the server itself emits — written in a format both humans and tools understand — and a browser explorer that renders it as a navigable, executable view of the API. The two pieces that fill those roles in ASP.NET Core are the **OpenAPI** specification and **Swagger UI**.

## The contract problem

A REST API exposes resources, operations, request shapes, response shapes, and authentication schemes. Each of those is information a client needs before it can issue a single useful request. Without a contract document the information lives in three unreliable places: the source code of the server, the documentation site somebody hand-wrote last quarter, and the institutional memory of whoever built the API.

All three drift. Source code is authoritative but unreadable to non-developers and inaccessible to clients written in other languages. Hand-written docs go stale the moment a controller changes. Institutional memory leaves with the people. The cost is paid every time a new client integrates: hours of reading code, slack threads asking for examples, and bug reports about responses that look nothing like what the docs promised.

The remedy is to derive the contract from the code automatically and publish it in a format that tools can consume. That format is OpenAPI.

## The OpenAPI specification

**OpenAPI** is the standard format (JSON or YAML) for describing an HTTP API's endpoints, request and response shapes, parameters, and authentication schemes; tooling consumes the OpenAPI document to generate documentation, client SDKs, and contract tests. It started life as the Swagger Specification at SmartBear, was donated to the Linux Foundation in 2015, and became OpenAPI 3.0 in 2017. Most production APIs target OpenAPI 3.0 or 3.1.

An OpenAPI document is one file. It declares the API's metadata (title, version, description), the servers it lives on, the security schemes it accepts, and a list of paths. Each path lists its operations — `get`, `post`, `put`, `patch`, `delete` — and each operation declares its parameters, request body, and possible responses. Reusable shapes go under `components/schemas` so a `Quote` defined once can be referenced from every operation that produces or consumes one.

A trimmed JSON document for a quotes API looks like the following.

```json
{
  "openapi": "3.0.1",
  "info": { "title": "CloudCiApi", "version": "v1" },
  "paths": {
    "/api/Quotes": {
      "get": {
        "tags": ["Quotes"],
        "responses": {
          "200": {
            "description": "Success",
            "content": {
              "application/json": {
                "schema": {
                  "type": "array",
                  "items": { "$ref": "#/components/schemas/QuoteDto" }
                }
              }
            }
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "QuoteDto": {
        "type": "object",
        "properties": {
          "id": { "type": "integer", "format": "int32" },
          "author": { "type": "string" },
          "text": { "type": "string" },
          "createdAt": { "type": "string", "format": "date-time" }
        }
      }
    }
  }
}
```

That document is the entire contract. A client developer reads it and knows what `GET /api/Quotes` returns. A code generator reads it and emits a typed C#, TypeScript, or Python client. A contract-testing tool reads it and asserts that the running server's responses still match. The single artifact serves humans and machines.

### Paths, operations, and schemas

The three building blocks repeat throughout. A **path** is a URI template (`/api/Quotes/{id}`), the parameterised address of a [resource](/course-book/6-services-and-apis/1-rest-principles/) or [resource collection](/course-book/6-services-and-apis/2-resource-modeling/). An **operation** is the combination of an HTTP method and a path — `GET /api/Quotes/{id}` is one operation, `POST /api/Quotes` is another. Each operation has its own parameters, request body schema, and response schemas. A **schema** describes the shape of data — usually a JSON object with named, typed properties, but also arrays, primitives, enums, and references to other schemas.

Schemas are where the [DTO](/course-book/6-services-and-apis/3-dtos-vs-entities/) discipline pays off. The OpenAPI document references `QuoteDto` and `CreateQuoteRequest` — the wire types — never the entity. A reader of the contract sees only what the API speaks, never the persistence model.

### Security schemes

OpenAPI describes authentication as well as data. A document that secures every operation behind a [bearer token](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/) declares a security scheme:

```json
{
  "components": {
    "securitySchemes": {
      "Bearer": {
        "type": "http",
        "scheme": "bearer",
        "bearerFormat": "JWT"
      }
    }
  },
  "security": [{ "Bearer": [] }]
}
```

An [API key](/course-book/5-identity-and-security/6-api-keys/) scheme reads as `"type": "apiKey"` with `in` set to `header` or `query` and a `name` for the header. Once a security scheme is declared, tooling treats it as a first-class part of the contract — Swagger UI grows an **Authorize** button, code generators emit a parameter for the credential, and contract tests fail loudly if a route that should require auth doesn't.

## Swagger UI as the browser explorer

The OpenAPI document is machine-readable but not pleasant to read by hand. **Swagger UI** is the browser-rendered explorer that reads an OpenAPI document and displays each endpoint with its parameters and request/response examples, with a "Try it out" button that issues real requests; in ASP.NET Core it is added by the Swashbuckle package and served at `/swagger`.

The page renders one tag per group of related operations (typically one per controller), each expandable into its operations. Expanding `POST /api/Quotes` shows the request body schema with example JSON, the possible responses, and a **Try it out** button. Clicking the button turns the operation into a form: the user fills in the request body, hits **Execute**, and the page shows the actual `curl` command, the response status, the response headers, and the response body.

For an API still being built this is invaluable. The same page that documents the contract is also the simplest test client. A backend developer adds an action, refreshes `/swagger`, and the new operation is right there to exercise — no Postman setup, no `curl` arguments to remember, no separate test harness. For a deployed API it is a discovery surface: a colleague asks "what does the quotes API do?" and the answer is a URL, not a screen-share.

The trade-off is exposure. Swagger UI reveals every route, parameter, and response shape — fine for a known public contract, reconnaissance for an attacker against an internal admin API. The conventional middle ground is to ship the OpenAPI JSON at `/swagger/v1/swagger.json` for tooling but gate or remove the interactive UI in production, or keep both behind authentication.

## Swashbuckle generates OpenAPI from the controller code

ASP.NET Core does not produce OpenAPI by hand. The **Swashbuckle.AspNetCore** package introspects the controller assembly at runtime, walks every action method, and emits an OpenAPI document derived from what it finds. The route attribute becomes the path. The HTTP method attribute becomes the operation. The parameters of the action method become the operation's parameters and request body. The action's return type becomes the success response schema.

Two service registrations and two middleware calls are everything an out-of-the-box setup needs.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
```

`AddEndpointsApiExplorer` registers the metadata source that Swagger reads — without it, `AddSwaggerGen` has nothing to introspect and the resulting document is empty. `AddSwaggerGen` registers the document generator. `UseSwagger` serves the document at `/swagger/v1/swagger.json`. `UseSwaggerUI` serves the browser explorer at `/swagger`. Both middleware calls must register before `MapControllers` — the endpoint routing terminates the pipeline, and middleware downstream of it never runs.

That is the whole contract for a basic API. From there, every additional piece of metadata in the controller — DTO types, return-type attributes, XML doc comments — flows into a richer document with no extra wiring.

### Annotations that improve the document

The default-generated document is correct but sparse. The action's declared return type is the only response shape Swagger sees, so an action returning `ActionResult<QuoteDto>` documents `200 OK → QuoteDto` and nothing about the `404` it might return. `[ProducesResponseType]` fills in the gaps:

```csharp
[HttpGet("{id:int}")]
[ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public ActionResult<QuoteDto> GetById(int id)
{
    var quote = _store.GetById(id);
    return quote is null ? NotFound() : Ok(ToDto(quote));
}
```

Each attribute adds one response entry to the operation's `responses` map. The `200` carries a `QuoteDto` schema; the `404` carries no body, just a status. A reader of the contract now knows both code paths exist.

XML documentation comments add prose. With `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the `.csproj` and `c.IncludeXmlComments(xmlPath)` in `AddSwaggerGen`, the `<summary>` on an action becomes the operation's description in Swagger UI, and the `<remarks>` becomes the long-form text. Property comments on a DTO become the schema's property descriptions. The same triple-slash comments developers already write for IntelliSense flow into the contract document.

### Exposing authentication so "Authorize" works

A controller decorated with `[Authorize]` rejects unauthenticated requests at runtime, but Swashbuckle does not know how the authentication is supposed to happen — bearer token, API key, OAuth flow, mutual TLS. The author tells it explicitly with `AddSecurityDefinition` and `AddSecurityRequirement`:

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste a JWT obtained from the token endpoint."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

Two effects follow. The OpenAPI document gains the security scheme and the operation-level requirement, so generated clients know to attach an `Authorization: Bearer <token>` header. Swagger UI gains the **Authorize** button at the top of the page; clicking it opens a dialog where the user pastes a token, and from that moment every "Try it out" call against a secured operation includes the header automatically. A second `AddSecurityDefinition` for an API key — `Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Header, Name = "X-Api-Key"` — produces a second authentication option in the same dialog.

This is what makes Swagger UI a real test client for a secured API. Without the security definition, every secured operation returns `401` from inside the explorer, and developers have to fall back to `curl` or Postman to exercise auth-protected routes.

## The toolchain payoff

The OpenAPI document is a one-time effort that pays out in many directions.

**Auto-generated client SDKs.** Tools like NSwag, OpenAPI Generator, and Kiota read the OpenAPI document and emit a typed client in the target language. A C# project consumes the API by adding a generated class with methods like `GetQuotesAsync()` and `CreateQuoteAsync(CreateQuoteRequest)` — no hand-written `HttpClient` plumbing, no JSON serialisation boilerplate, and a compile-time error if the contract changes in a breaking way. A TypeScript front end gets the same treatment with the same source document.

**Contract tests.** A test runner like Schemathesis or Dredd reads the OpenAPI document and exercises every operation against the running service, asserting that responses match the declared schemas. A response that drops a property, changes a field type, or returns an undocumented status code fails the test. The contract is no longer aspirational — it is enforced.

**Postman and Insomnia imports.** Both tools accept an OpenAPI document and produce a complete request collection from it, with all routes, example bodies, and authentication wired up. A new team member is one URL away from a working request library.

**Mock servers.** Tools like Prism start an HTTP server from an OpenAPI document and return example responses for every operation. A front-end team builds against the mock while the back end is still being implemented; both sides agree on the contract first, then implement against it independently.

| Use of the OpenAPI document | What it produces |
|------|------|
| Swagger UI | Browser-rendered explorer with "Try it out" |
| Code generator (NSwag, Kiota, openapi-generator) | Typed client SDK in C#, TypeScript, Python, etc. |
| Contract tester (Schemathesis, Dredd) | Pass/fail assertions that responses match schemas |
| Postman / Insomnia import | A complete request collection |
| Mock server (Prism) | A stand-in HTTP server for parallel development |

Each consumer reads the same document. Generating it once from the controller code — and keeping it accurate with `[ProducesResponseType]` and security definitions — unlocks all five.

## Worked example tied to the exercise

The companion exercise [REST Controllers, DTOs, and Swagger](/exercises/4-services-and-apis/1-rest-api-and-dtos/1-rest-controllers-and-dtos/) builds a `CloudCiApi` Web API around a `Quotes` resource and wires Swashbuckle exactly as described above. The Swagger setup in that exercise is intentionally minimal — `AddEndpointsApiExplorer`, `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI` — because the controller, the route attributes, and the DTO types carry all the information Swashbuckle needs to produce a usable document on day one. The exercise also leaves Swagger enabled in production, making the deployed `/swagger` page the API's demo URL. Subsequent exercises in the same chapter add an API key middleware and a JWT bearer scheme; each of those exercises extends the Swashbuckle configuration with the corresponding `AddSecurityDefinition` so the **Authorize** button in Swagger UI works for the new scheme.

The takeaway from running through that exercise is that OpenAPI is not a deliverable a team writes after the API is built. It is a by-product of writing the controller carefully — naming the action, declaring the route, returning a DTO, annotating the response types, configuring the security scheme. Swashbuckle then generates the document for free, and Swagger UI renders it.

## Summary

OpenAPI is the standard JSON or YAML format for describing an HTTP API's endpoints, request and response shapes, parameters, and authentication schemes. Swagger UI is the browser-rendered explorer that reads an OpenAPI document and turns it into a navigable, executable surface. In ASP.NET Core, the Swashbuckle package introspects controllers, DTOs, attributes, and XML comments to generate the document at runtime — `AddSwaggerGen`, `UseSwagger`, and `UseSwaggerUI` are the entire wire-up. `[ProducesResponseType]` documents non-success responses; `AddSecurityDefinition` exposes bearer and API key schemes so Swagger UI's **Authorize** button can attach credentials to "Try it out" calls. The same OpenAPI document feeds code generators, contract testers, Postman imports, and mock servers, turning one piece of metadata into a whole toolchain. The contract becomes a living artifact — derived from the code, consumed by tools, and trustworthy because it cannot drift away from what the server actually does.
