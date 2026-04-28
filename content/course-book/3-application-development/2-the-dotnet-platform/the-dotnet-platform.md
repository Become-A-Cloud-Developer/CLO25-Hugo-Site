+++
title = "The .NET Platform"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 20
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/3-application-development/2-the-dotnet-platform.html)

[Se presentationen på svenska](/presentations/course-book/3-application-development/2-the-dotnet-platform-swe.html)

---

Building a web application requires a language to express logic, a runtime to execute that logic, libraries to handle common concerns, and a packaging system to share code across projects. .NET provides all four as a single, integrated platform. Understanding how the pieces fit together — from C# source code through compilation to a running web server — gives the foundation for every other chapter in Part III.

## Why .NET exists

**.NET** is the cross-platform development platform from Microsoft for building applications in C# (and other languages) that compile to a common Intermediate Language and run on the .NET runtime; the unified version replaces the older .NET Framework. The platform addresses a recurring problem in application development: source code written for one operating system, language version, or hardware architecture often fails to run on another. Without a common execution model, every team rewrites the same plumbing — memory management, string handling, networking primitives, package distribution.

The original .NET Framework, released in 2002, solved this only for Windows. Code written against the Framework ran on Windows desktops and Windows Server, but not on Linux or macOS. As cloud workloads moved toward Linux containers and developers adopted macOS for daily work, this Windows-only constraint became a serious limitation. Microsoft responded with .NET Core in 2016 — a complete rewrite of the runtime designed to run identically on Windows, Linux, and macOS. After several release cycles, the Core lineage merged with the original Framework codebase into a unified product called .NET, with major version numbers continuing the Core sequence (5, 6, 7, 8, 9, and now 10).

The unified platform is open source, distributed under the MIT license, and developed in public on GitHub. The same SDK, compiler, and runtime power desktop apps, mobile apps, console tools, microservices, and the ASP.NET Core web stack used throughout this course.

## The compilation pipeline

C# is a statically typed language, which means the compiler verifies types and catches a category of errors before the code ever runs. The compiler does not produce machine code directly. Instead, it produces a portable intermediate representation that the runtime translates to machine code on the target machine.

The pipeline has four stages:

1. **C# source code** in `.cs` files — what the developer writes.
2. **Intermediate Language** (IL) — what the C# compiler emits. IL is a CPU-independent bytecode similar in spirit to Java bytecode. The same IL runs unchanged on x86-64, ARM64, Windows, Linux, and macOS.
3. **Just-in-time compilation** (JIT) — the runtime translates IL to native machine code the first time a method executes. Subsequent calls reuse the compiled code.
4. **Machine code** — the actual CPU instructions that execute on the host hardware.

This two-step model — compile once to IL, JIT-compile at runtime — explains why a single published .NET application can target multiple operating systems and CPU architectures from one codebase. The IL is platform-neutral; the JIT compiler embedded in each platform's runtime handles the platform-specific translation.

### The common language runtime

The **CLR** (Common Language Runtime) is the .NET execution environment that loads compiled assemblies, performs just-in-time compilation of Intermediate Language to machine code, and provides services such as garbage collection and exception handling. The CLR is what makes .NET a *managed* runtime: code does not allocate and free memory directly, and unhandled errors do not corrupt process state in unpredictable ways.

Two CLR services shape how C# code behaves day-to-day. The **garbage collector** tracks every object the program allocates on the managed heap and reclaims memory once no live references remain. Developers do not call `free()` or `delete` — the GC runs periodically, identifies unreachable objects, and frees their memory. This eliminates entire classes of bugs (use-after-free, double-free, memory leaks from forgotten cleanup) at the cost of occasional pauses while collection runs.

The CLR's exception handling model is the second pillar. When code throws an exception, the runtime walks the call stack looking for a matching `catch` clause. If none exists, the runtime terminates the process cleanly rather than allowing corrupted state to spread. Web frameworks like ASP.NET Core layer on top of this, catching exceptions at the request boundary so that one failed request does not crash the whole server.

## Assemblies and NuGet packages

Compiled .NET code is distributed as assemblies. An **assembly** is a compiled .NET unit — typically a `.dll` file — containing Intermediate Language, type metadata, and resources, that the CLR loads at runtime. A small console application produces one assembly. A larger web application produces one assembly for the application itself plus dozens or hundreds of dependency assemblies, each contributed by a different library.

The metadata embedded in every assembly is what makes the runtime self-describing. Tools can inspect a `.dll` and discover the types it defines, the methods on each type, the parameters of each method, and the attributes applied to each declaration. The DI container, the model binder, and the routing system all rely on this reflection capability.

Sharing assemblies across projects requires a package manager. **NuGet** is the package manager for .NET; published packages contain assemblies and metadata, and projects declare dependencies in their `.csproj` files for the SDK to restore. Public packages live on `nuget.org`; private feeds (Azure Artifacts, GitHub Packages) host internal libraries. The exercises in this course pull packages such as `Microsoft.AspNetCore.Mvc`, `MongoDB.Driver`, and `Azure.Storage.Blobs` from `nuget.org`.

### The .NET 10 SDK and the dotnet CLI

The .NET 10 SDK is the toolchain installed on the developer's machine. It bundles the C# compiler, the runtime, the project templates, and the `dotnet` command-line interface. Every project in this course is created, built, and run through `dotnet` commands rather than through an IDE-specific menu, which keeps the workflow identical on Windows, Linux, and macOS.

Five commands cover the bulk of daily work:

| Command | Purpose |
|---------|---------|
| `dotnet new <template>` | Scaffold a new project from a template (`mvc`, `console`, `webapi`, `classlib`). |
| `dotnet restore` | Download all NuGet dependencies declared in the `.csproj` file. |
| `dotnet build` | Compile the project to IL, producing assemblies in `bin/Debug/`. |
| `dotnet run` | Build (if needed) and execute the project locally. |
| `dotnet add package <name>` | Add a NuGet package reference and trigger restore. |

`dotnet run` and `dotnet build` invoke `dotnet restore` automatically when dependencies are missing, so an explicit restore is rarely needed in a fresh clone — running the project triggers the chain.

## ASP.NET Core as the web layer

The CLR and the SDK handle compilation and execution, but a web application also needs an HTTP server, a routing system, a way to render HTML, and an extension point for middleware (logging, authentication, compression). **ASP.NET Core** is the .NET web framework that hosts HTTP applications using a configurable request-handling pipeline, with built-in support for the MVC pattern, dependency injection, configuration, and middleware.

ASP.NET Core ships with Kestrel, a high-performance HTTP server written in C#. When `dotnet run` starts an ASP.NET Core project, Kestrel binds to a port (typically 5000 for HTTP and 7240 for HTTPS in development), accepts incoming connections, parses each [HTTP request](/course-book/3-application-development/1-http-fundamentals/), and hands it off to the request pipeline configured in `Program.cs`. The pipeline routes the request to a controller action, the action returns a result, and Kestrel writes the response back to the client.

The framework is modular. Features such as authentication, session state, CORS, and response caching are opt-in middleware components, registered in `Program.cs`. A minimal API service may use only routing and JSON serialization; a full MVC application composes a longer pipeline with view rendering, anti-forgery tokens, and authorization checks.

## A worked example: scaffolding an MVC project

The companion exercise [Building the Presentation Layer](/exercises/10-webapp-development/1-presentation-layer/) starts by scaffolding a new MVC application. The single command does the work:

```bash
dotnet new mvc -n CloudSoft
```

The command generates the following folder structure:

```text
CloudSoft/
├── CloudSoft.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Controllers/
│   └── HomeController.cs
├── Models/
│   └── ErrorViewModel.cs
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml
│   │   └── Privacy.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _ValidationScriptsPartial.cshtml
│   │   └── Error.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── lib/
│   └── favicon.ico
└── Properties/
    └── launchSettings.json
```

Each top-level folder corresponds to one concept introduced in this Part. `CloudSoft.csproj` declares the project's target framework (`net10.0`), the SDK (`Microsoft.NET.Sdk.Web`), and the NuGet package references — the SDK uses this file to drive `restore`, `build`, and `run`. `Program.cs` is the entry point: it builds the web host, registers services with the DI container, configures the middleware pipeline, and calls `app.Run()` to start Kestrel. `Controllers/` and `Views/` together form the MVC presentation layer; `wwwroot/` holds static assets that Kestrel serves directly without invoking a controller. The `appsettings.json` files supply configuration values that ASP.NET Core loads at startup.

Running `dotnet run` from the `CloudSoft/` directory triggers the full pipeline: the SDK restores NuGet packages, the C# compiler emits `CloudSoft.dll` (an assembly) into `bin/Debug/net10.0/`, the runtime starts the CLR, the CLR loads the assembly, JIT-compiles `Program.Main` to machine code, and Kestrel begins listening for HTTP requests. Visiting `https://localhost:7240` in a browser produces the welcome page rendered by `HomeController.Index()` and `Views/Home/Index.cshtml`.

## .NET compared with other web stacks

Choosing a platform involves trade-offs across performance, ecosystem, hiring pool, and operational maturity. .NET sits in a similar slot to the JVM (Java, Kotlin) and Go for backend web work, with different strengths.

| Platform | Compilation | Memory model | Typical web framework |
|----------|-------------|--------------|------------------------|
| .NET | C# → IL → JIT to native | Garbage collected | ASP.NET Core |
| JVM | Java → bytecode → JIT to native | Garbage collected | Spring Boot |
| Go | Source → native at build time | Garbage collected | Standard library `net/http` |
| Node.js | JavaScript interpreted by V8 (with JIT) | Garbage collected | Express, Fastify |

Cloud-native services on Azure benefit from .NET's first-party tooling (Azure SDK, Application Insights, Azure Functions runtime) and the deepest documentation surface. The exercises in this course standardise on .NET so that the platform itself never becomes the topic — the topic is the architectural pattern being learned.

## Summary

.NET is the cross-platform managed runtime that executes C# code through a compile-to-IL, JIT-to-native pipeline. The CLR provides the execution environment, including garbage collection and exception handling, so developers focus on application logic rather than memory management. Compiled code ships as assemblies — `.dll` files containing IL and metadata — and is shared between projects through NuGet packages declared in `.csproj` files. The .NET 10 SDK supplies the `dotnet` CLI used to scaffold, build, and run projects (`dotnet new`, `dotnet build`, `dotnet run`, `dotnet add package`). ASP.NET Core layers on top of this foundation as the web framework, providing the Kestrel HTTP server, the MVC request pipeline, and the dependency injection container that subsequent chapters build on. Together, these pieces form the platform on which every exercise in Part III is built.
