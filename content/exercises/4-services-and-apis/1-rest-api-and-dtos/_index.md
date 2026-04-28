+++
title = "1. REST API and DTOs"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Build a controllers-based REST API with DTOs and Swagger, then secure it first with an API-key middleware and finally with JWT bearer authentication. Three exercises that share one quotes API and progressively shift its identity model from anonymous to keyed to authenticated."
weight = 1
+++

# REST API and DTOs

The goal of this chapter is one HTTP API — `CloudCiApi`, a small quotes service with three endpoints — taken through three identity models. The same `Quote` domain (id, author, text, createdAt) survives all three exercises; what changes is the shape of the application around it. The first exercise builds the *surface*: **REST controllers**, separate **DTOs** and entity types, and a **Swagger** UI that renders the endpoints. The second exercise adds the *gate*: an **API key middleware** that rejects requests without a valid header. The third exercise replaces that gate with a real *identity model*: **JWT bearer** tokens minted by a `TokensController` and enforced with `[Authorize]`.

The arc moves from anonymous, to keyed, to authenticated:

The first exercise scaffolds a Web API project with controllers, splits the wire shape from the persistence shape using DTOs, and exposes everything through Swagger UI so endpoints are explorable in the browser. The CI/CD pipeline from the previous deployment chapter is reused as-is — the chapter opens with a terse abbreviated setup that points back to the earlier chapter for the full explanation, so you can spend the time on API design rather than redoing infrastructure. The second exercise gates the endpoints behind an `ApiKeyMiddleware` that compares an `X-Api-Key` header against a configured value. The key reaches production through the same Container Apps secret plus `secretref:` env var pattern you already learned, and Swagger UI is wired to support its "Authorize" flow so you can still hit the endpoints from the browser. The third exercise replaces the API key with **JWT bearer** authentication: a `TokensController` issues signed JWTs against an in-memory user list, the quotes controller takes `[Authorize]` to enforce the new model, and the chapter ends by tearing down the Week 6 resource group along with the new Entra OIDC app — mirroring the cleanup pattern at the end of the previous deployment chapter so you finish with a clean slate.

> ℹ **Where this fits**
>
> This subsection sits inside the broader **Services and APIs** chapter. It introduces the building blocks every HTTP API needs — controllers, DTOs, Swagger — and the two most common ways to put a gate in front of one: an API key for machine-to-machine calls, and JWT bearer for end-user authentication. A future chapter will likely tackle service-to-service calls, downstream dependencies, and OpenAPI-generated clients; this one stays focused on the server side. The previous **Logging and Monitoring** chapter is the immediately preceding context — you already have Application Insights wired up via `secretref:` and an OIDC pipeline shipping containers to Azure, so this chapter builds on that infrastructure rather than re-teaching it.

{{< children />}}
