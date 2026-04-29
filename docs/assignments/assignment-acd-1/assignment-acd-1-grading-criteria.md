# Grading Rubric: Assignment 1 (ACD)

## Instructions for Evaluators

This rubric is used to evaluate student submissions for ACD Assignment 1. Evaluate each criterion independently. The overall grade is determined as follows:

- **Väl Godkänt (VG):** All criteria are met at Godkänt level AND at least 5 of 7 criteria are met at VG level.
- **Godkänt (G):** All criteria are met at Godkänt level.
- **Underkänt (U):** One or more criteria are not met at Godkänt level.

For each criterion, assess the student's report and repository against the indicators listed below. Provide a brief justification for each assessment.

The assignment is intentionally open-ended. The distinction between Godkänt and Väl Godkänt is mainly the **depth of exploration and the quality of motivation** behind the student's choices, not the presence of additional features.

---

## Criterion 1: User Stories and Inner Loop

**Learning objectives:** Plan and conduct the deployment process for a containerized application, showing how development and operations interact.

**Godkänt:**

- 3–5 user stories are provided in a recognizable role/want/value format and relate to the application's actual features.
- The student describes their inner loop — what tools and commands they use during development.

**Väl Godkänt:**

- User stories include acceptance criteria or are clearly aligned with the features actually delivered in the repository.
- The student reflects on how the inner loop changes when local dependencies (database, storage emulator) are introduced, and on the trade-offs between speed of feedback and fidelity to production.

---

## Criterion 2: Containerization and Local Development

**Learning objectives:** Package, develop, and deploy a containerized application on a cloud platform.

**Godkänt:**

- A working Dockerfile is provided that produces a runnable image of the application.
- A Docker Compose configuration runs the application together with the dependent services it needs locally (e.g., MongoDB).
- The image is published to a registry (Azure Container Registry preferred).

**Väl Godkänt:**

- The Dockerfile uses a multi-stage build that separates build-time tooling from the runtime image, and the student explains the layering and caching choices made.
- The local development environment is described as something a teammate could pick up and run with a single command.
- The student motivates the choice of registry and discusses image size, layer reuse, or related trade-offs.

---

## Criterion 3: Authentication and Authorization

**Learning objectives:** Apply security principles. Describe lifecycle and security aspects for cloud applications.

**Godkänt:**

- The application uses ASP.NET Core Identity for user management.
- At least two roles (e.g., Admin, Candidate) are implemented and protected pages enforce them.
- CSRF protection is enabled on state-changing endpoints.
- The student describes who the users are and what they may do.

**Väl Godkänt:**

- The first administrator is seeded through configuration rather than manually inserted in the database.
- The student demonstrates an understanding of why each measure matters — discusses cookie settings, the difference between authentication and authorization, or specific threats CSRF protects against.
- Security choices are presented as deliberate decisions, not as a feature checklist.

---

## Criterion 4: Data Layer

**Learning objectives:** Use appropriate cloud services. Describe lifecycle for cloud applications.

**Godkänt:**

- The application persists domain data using MongoDB locally and Azure Cosmos DB (MongoDB API) in production.
- The connection string is supplied through configuration and is not committed to the repository.
- The student describes the data model at a high level.

**Väl Godkänt:**

- The student explains how the same code path works against both Mongo (local) and Cosmos DB (production), and what differences they are aware of.
- The connection string flows through a clearly described configuration chain — environment variables, Container App secrets, or similar — and the student motivates the chosen approach.
- The student demonstrates understanding of where data lives and what would happen during a redeploy.

---

## Criterion 5: CI/CD Pipeline

**Learning objectives:** Implement an automated deployment flow via CI/CD and document how it works.

**Godkänt:**

- A GitHub Actions workflow builds the application, builds a Docker image, pushes it to a registry, and deploys a new revision to Azure Container Apps.
- The workflow runs on push to the main branch (or an equivalent trigger that the student motivates).
- The student describes the pipeline and what each step does.

**Väl Godkänt:**

- The pipeline authenticates to Azure through OIDC federated identity rather than long-lived service principal secrets.
- The pipeline includes an automated verification step that confirms the new revision is healthy (e.g., HTTP probe, revision activation check).
- The student explains the workflow as an end-to-end story — what triggers it, what artifacts move where, and what would happen on a failed step.

---

## Criterion 6: Security and Infrastructure as Code (cross-cutting)

**Learning objectives:** Apply security principles. Use Infrastructure as Code tools to create, update, and roll out environments.

**Godkänt:**

- No secrets are committed to the repository.
- Azure resources (resource group, ACR, Container Apps environment, Container App) are documented in scripts so the environment can be recreated, even if some steps are manual.
- The student addresses security at more than one layer (e.g., application, registry, deployment).

**Väl Godkänt:**

- Infrastructure is defined in IaC (Bicep or a complete Azure CLI script) and is repeatable without manual intervention.
- Identities are scoped following least privilege — the workflow's identity has only the roles it needs.
- Security is discussed throughout the report, with deliberate decisions visible in the configuration (cookie flags, role assignments, registry access, image source).

---

## Criterion 7: Report Quality and AI-Assistant Reflection

**Learning objectives:** Document and communicate a technical solution. Describe how AI assistants are used as part of producing the solution.

**Godkänt:**

- The report is a single PDF with the required first-page elements (name, screenshot with URL, repository link).
- All sub-tasks are addressed.
- Diagrams are the student's own work; code is in monospace and copy-pasteable.
- The student includes a reflection on how they used AI assistants during the assignment.

**Väl Godkänt:**

- The report reads as a cohesive narrative focused on decisions and motivations rather than commands.
- The scope is explicitly defined and motivated — what is in, what is out, and why.
- The repository is well-organized and complements the report; a reader can move between the two without friction.
- The AI-assistant reflection goes beyond surface use — the student describes specific tasks AI helped with, where it failed, and how they validated suggestions.
