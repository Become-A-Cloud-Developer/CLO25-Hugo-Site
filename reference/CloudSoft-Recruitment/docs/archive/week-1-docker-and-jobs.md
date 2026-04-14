# Week 1: Docker and Job Management

## Purpose

Introduce containerization fundamentals and build the core Job CRUD functionality. Students set up their local development environment with Docker Compose and implement the MVC pattern with MongoDB.

## What Students Build

1. **Project scaffolding** — .NET 10 MVC solution with test project
2. **Job entity** — Domain model with MongoDB BSON attributes and validation
3. **Repository pattern** — `IJobRepository` with both MongoDB and InMemory implementations
4. **Service layer** — `JobService` with business logic (deadline validation, CRUD operations)
5. **Job controller and views** — Full CRUD with Bootstrap 5 card-based UI
6. **Unit tests** — 9 tests for JobService using InMemoryJobRepository
7. **Docker setup** — Multi-stage Dockerfile, Docker Compose with MongoDB and Azurite

## How to Run Locally

### Option 1: Docker Compose (recommended)

```bash
docker compose up -d
# App: http://localhost:5000
# MongoDB: localhost:27017
# Azurite: localhost:10000-10002
```

### Option 2: .NET CLI (in-memory mode)

```bash
dotnet run --project src/CloudSoft.Web
# Uses in-memory repository (default in appsettings.json)
```

### Option 3: .NET CLI with MongoDB

```bash
# Start MongoDB separately
docker run -d -p 27017:27017 --name mongodb mongo:latest

# Run with Development settings (UseMongoDB: true)
dotnet run --project src/CloudSoft.Web --launch-profile https
```

## End State

After Week 1, students have:

- A working MVC application with Job CRUD
- Docker Compose running MongoDB, Azurite, and the app
- 9 passing unit tests
- Feature flag toggling between InMemory and MongoDB repositories

## Key Learning

- **Docker fundamentals**: Dockerfile, multi-stage builds, Docker Compose, volumes, networking
- **MVC pattern**: Controllers, Views, Models with validation attributes
- **Repository pattern**: Interface-based data access with dependency injection
- **Service layer**: Business logic separated from controllers
- **MongoDB**: BSON attributes, document storage, MongoDB Driver for .NET
- **Testing**: xUnit, testing with in-memory implementations
