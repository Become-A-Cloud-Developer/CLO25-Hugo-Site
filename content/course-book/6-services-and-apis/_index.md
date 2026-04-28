+++
title = "Part VI — Services and APIs"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Designing HTTP APIs: REST principles, resource modeling, DTOs vs entities, status codes and versioning, OpenAPI, and pagination/idempotency/rate limiting."
weight = 60
chapter = true
head = "<label>Part VI</label>"
+++

# Part VI — Services and APIs

The ACD course's first deep dive into API design. Six chapters cover the conceptual ground (REST, resources, DTOs, status codes), the documentation layer (OpenAPI and Swagger), and the operational concerns that separate a toy API from a production one (pagination, idempotency, rate limiting).

The companion exercise is the [REST API and DTOs chapter](/exercises/4-services-and-apis/1-rest-api-and-dtos/), which builds the same `CloudCiApi` quotes service through three identity models — anonymous, API key, JWT — while applying every principle covered here.

{{< children />}}
