+++
title = "The .NET Platform"
program = "CLO"
cohort = "25"
courses = ["BCD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## The .NET Platform
Part III — Application Development

---

## Why .NET Exists
- **Cross-platform** managed runtime for Windows, Linux, and macOS.
- One toolchain for desktop, mobile, console, and web workloads.
- Open source under the MIT license; developed in public on GitHub.
- Unified version (5, 6, 7, 8, 9, 10) replaces the older Windows-only .NET Framework.

---

## The Compilation Pipeline
- **C# source** in `.cs` files written by the developer.
- C# compiler emits **Intermediate Language** (IL) — CPU-independent bytecode.
- **Just-in-time** (JIT) compilation translates IL to native code at runtime.
- Same IL runs on x86-64, ARM64, Windows, Linux, and macOS.

---

## The Common Language Runtime
- **CLR** loads assemblies, JIT-compiles IL, and hosts the running process.
- **Garbage collector** reclaims unreferenced memory automatically.
- **Exception handling** unwinds the stack and protects process state.
- Managed runtime eliminates use-after-free and double-free defects.

---

## Assemblies and NuGet
- An **assembly** is a `.dll` containing IL, type metadata, and resources.
- Metadata enables reflection — DI, model binding, and routing rely on it.
- **NuGet** is the package manager; dependencies declared in `.csproj`.
- Public packages on `nuget.org`; private feeds for internal libraries.

---

## The dotnet CLI
- `dotnet new <template>` — scaffold a new project (`mvc`, `webapi`, `console`).
- `dotnet restore` — download declared NuGet dependencies.
- `dotnet build` — compile to IL, output to `bin/Debug/`.
- `dotnet run` — build and start the application locally.
- `dotnet add package <name>` — add a NuGet reference.

---

## ASP.NET Core
- The .NET **web framework** layered on top of the CLR.
- **Kestrel** HTTP server accepts connections and parses requests.
- Configurable middleware **pipeline** in `Program.cs`.
- Built-in support for **MVC**, dependency injection, and configuration.

---

## Worked Example: dotnet new mvc
- `dotnet new mvc -n CloudSoft` scaffolds an MVC project.
- `Controllers/`, `Views/`, `Models/` form the presentation layer.
- `Program.cs` builds the host and starts Kestrel.
- `appsettings.json` supplies configuration; `wwwroot/` holds static assets.

---

## Questions?
