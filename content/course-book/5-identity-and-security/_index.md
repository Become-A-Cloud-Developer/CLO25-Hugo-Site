+++
title = "Part V — Identity and Security"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
description = "Authentication, authorization, and secret management: cookies, JWT, ASP.NET Core Identity, OAuth, OpenID Connect, Key Vault, and Managed Identities."
weight = 50
chapter = true
head = "<label>Part V</label>"
+++

# Part V — Identity and Security

The Part the course returns to most often. Every web application needs to answer two questions on every protected request: who is asking, and are they allowed? This Part covers both questions, the building blocks the framework provides for each, and the cloud-side machinery for keeping the secrets those building blocks rely on.

The eight chapters move from the conceptual split (Ch 1) through the most common browser-facing pattern (cookies + Identity, Ch 2–4), into the API-facing patterns (JWT and API keys, Ch 5–6), out to delegated authentication (OAuth and OIDC, Ch 7), and end with secret management (Ch 8). Companion exercises include the [authentication and authorization exercise](/exercises/10-webapp-development/4-authentication-authorization/) and the [identity and user stores exercise](/exercises/10-webapp-development/5-identity-and-user-stores/).

{{< children />}}
