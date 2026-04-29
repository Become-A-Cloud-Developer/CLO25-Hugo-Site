+++
title = "Week 6 (v.20)"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "REST APIs and DTOs: API controllers next to MVC, DTOs vs entities, Swagger, JWT, API keys"
weight = 6
+++

# Week 6 (v.20) — REST API and DTOs

Add a REST API surface alongside the MVC views. Separate DTOs from entities, document the API with Swagger/OpenAPI, and protect endpoints with API keys and JWT bearer tokens.

## Theory

- [Part VI — Services and APIs](/course-book/6-services-and-apis/)
  - [REST Principles](/course-book/6-services-and-apis/1-rest-principles/rest-principles/)
  - [Resource Modeling](/course-book/6-services-and-apis/2-resource-modeling/resource-modeling/)
  - [DTOs vs Entities](/course-book/6-services-and-apis/3-dtos-vs-entities/dtos-vs-entities/)
  - [Status Codes and Errors](/course-book/6-services-and-apis/4-status-codes-and-errors/status-codes-and-errors/)
  - [OpenAPI and Swagger](/course-book/6-services-and-apis/5-openapi-and-swagger/openapi-and-swagger/)
  - [Pagination and Rate Limiting](/course-book/6-services-and-apis/6-pagination-and-rate-limiting/pagination-and-rate-limiting/)
- [Part V — Identity and Security](/course-book/5-identity-and-security/)
  - [Bearer Tokens and JWT](/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/bearer-tokens-and-jwt/)
  - [API Keys](/course-book/5-identity-and-security/6-api-keys/api-keys/)

## Practice

- [Services and APIs — REST API and DTOs](/exercises/4-services-and-apis/1-rest-api-and-dtos/) — three progressive exercises
  - [REST controllers and DTOs](/exercises/4-services-and-apis/1-rest-api-and-dtos/1-rest-controllers-and-dtos/)
  - [API key middleware](/exercises/4-services-and-apis/1-rest-api-and-dtos/2-api-key-middleware/)
  - [JWT bearer and cleanup](/exercises/4-services-and-apis/1-rest-api-and-dtos/3-jwt-bearer-and-cleanup/)

## Preparation

- Read up on REST principles and JSON
- Familiarize yourself with Swagger/OpenAPI

## Reflection Questions

- Why separate DTOs from database entities?
- How does JWT authentication differ from cookie-based login?
- What is the purpose of Swagger in an API project?

## Links

- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api)
- [Swagger/OpenAPI](https://swagger.io)
