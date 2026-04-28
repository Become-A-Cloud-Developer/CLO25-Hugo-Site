+++
title = "Configuration and Environments"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 50
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/3-application-development/5-configuration-and-environments.html)

[Se presentationen på svenska](/presentations/course-book/3-application-development/5-configuration-and-environments-swe.html)

---

An application that runs on a developer's laptop, on a build server, and in a production cluster needs different connection strings, different log levels, and different credentials in each location. Hardcoding these values into source code forces a recompile for every environment and pushes secrets into version control. ASP.NET Core solves this by separating settings from code and reading them through a layered configuration system that resolves the right value for the environment the process is running in. This chapter explains how that system is assembled, how strongly-typed bindings expose it to application code, and where the boundary between development-time and production-time secret storage falls.

## Why configuration is separate from code

The [twelve-factor methodology introduced in Part I](/course-book/1-cloud-foundations/4-cloud-native-development/3-the-12-factor-model/) treats configuration as anything that varies between deploys: hostnames, credentials, feature flags, log levels. Code that contains these values directly cannot move between environments without modification, which means the artifact tested in development is not the artifact deployed to production. Externalizing configuration restores that property — the same compiled binary boots with development settings on one machine and production settings on another, and only the surrounding values differ.

Three forces push the same direction. Secrets must not enter source control because git history is permanent and repository access is broader than production access. Operators need to change settings without rebuilding the application, which rules out compiled constants. Multiple deploys of the same binary — a staging slot and a production slot, or two regions — must coexist with different configuration without divergent code paths. ASP.NET Core's configuration system addresses all three by treating settings as data the host loads at startup.

## The configuration provider chain

ASP.NET Core builds configuration from a chain of **configuration providers**, each contributing key-value pairs into a single hierarchical tree. The default chain that `WebApplication.CreateBuilder(args)` registers is, in order:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User-secrets (only when the environment is `Development`)
4. Environment variables
5. Command-line arguments

Providers are read in order and later providers override earlier ones for any key they supply. A connection string defined in `appsettings.json` is replaced by the same key in `appsettings.Production.json` when the production environment is active, and replaced again if an environment variable with the matching name is set. Keys that no provider supplies simply do not exist; reading them returns `null`.

### IConfiguration as the read interface

**`IConfiguration`** is the ASP.NET Core abstraction that reads settings from a chain of providers — `appsettings.json`, environment-specific overrides, environment variables, command-line arguments, and user-secrets — and exposes them as a hierarchical key-value tree. Application code receives `IConfiguration` through dependency injection (covered in [the dependency injection chapter](/course-book/3-application-development/6-dependency-injection/)) and reads values by key path:

```csharp
public class HomeController : Controller
{
    private readonly string _connectionString;

    public HomeController(IConfiguration configuration)
    {
        _connectionString = configuration["MongoDB:ConnectionString"];
    }
}
```

The colon in `"MongoDB:ConnectionString"` is the section separator. ASP.NET Core treats the configuration tree as nested objects; a JSON file with `{ "MongoDB": { "ConnectionString": "..." } }` produces the key path `MongoDB:ConnectionString` regardless of which provider supplied it. The consumer never asks which file or environment variable a value came from — the abstraction collapses the chain into a single lookup.

### Appsettings.json and environment-specific files

**`appsettings.json`** is the default configuration file shipped with an ASP.NET Core project; environment-specific files such as `appsettings.Development.json` and `appsettings.Production.json` layer on top of it and override individual keys when the matching environment is active. The base file holds settings shared across all environments — log categories, default feature flags, structural defaults — and the environment file holds only the deltas. A developer who opens `appsettings.Development.json` sees a small file containing only what differs from the base, not a full duplicate of every setting.

The file naming follows a strict convention. The host reads the `ASPNETCORE_ENVIRONMENT` variable, looks for `appsettings.<value>.json`, and merges it on top of `appsettings.json`. Common values are `Development`, `Staging`, and `Production`, but any string is allowed; teams that run integration test suites against a dedicated environment often define `appsettings.IntegrationTest.json` and set `ASPNETCORE_ENVIRONMENT=IntegrationTest` for that pipeline stage.

## Environment selection

The `ASPNETCORE_ENVIRONMENT` environment variable controls which environment-specific configuration file the host loads and which behavioural defaults the framework applies. When the variable is unset, ASP.NET Core defaults to `Production`, which is a deliberate safety choice — a misconfigured server is less dangerous when it assumes production-grade defaults than when it assumes development-grade ones such as detailed exception pages.

Three behaviours hinge on the environment value. The matching `appsettings.<env>.json` file is loaded. The user-secrets provider is registered only when the environment is `Development`. Middleware checks such as `app.Environment.IsDevelopment()` enable developer-only features (the developer exception page, runtime compilation of Razor views) without those features ever activating in production. The same compiled binary therefore behaves differently based on a single environment variable, with no conditional compilation involved.

In Visual Studio and `dotnet run`, the environment defaults to `Development` because the launch profiles in `Properties/launchSettings.json` set the variable explicitly. In a published deployment, the hosting platform sets it — Azure App Service, Container Apps, and Kubernetes all expose the variable through their configuration UI or manifest.

## User-secrets for development

Storing a database password in `appsettings.Development.json` works until the file enters version control, at which point the password is exposed to anyone with repository access. ASP.NET Core's user-secrets feature solves this by moving sensitive values out of the project directory entirely.

**User-secrets** is a development-only configuration provider that stores sensitive values outside the project directory (in the user's profile) so credentials are not committed to source control; production environments rely on environment variables or a secret store instead. The secrets file lives at `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json` on Windows and `~/.microsoft/usersecrets/<id>/secrets.json` on macOS and Linux, where `<id>` is a GUID stored in the `.csproj` file under `UserSecretsId`. Because the file lives outside the project tree, no `git add` can include it.

The workflow uses the `dotnet user-secrets` CLI:

```bash
dotnet user-secrets init
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb://localhost:27017/cloudsoft"
dotnet user-secrets set "Storage:AccountKey" "dev-key-value"
dotnet user-secrets list
```

The `init` command writes a `UserSecretsId` GUID into the `.csproj` file. The `set` command stores a key-value pair in the user-profile JSON file, using the same colon-separated key paths as `appsettings.json`. At runtime, when `ASPNETCORE_ENVIRONMENT=Development`, the user-secrets provider reads that JSON file and exposes its keys through `IConfiguration`. Application code reads `configuration["MongoDB:ConnectionString"]` without knowing — or caring — whether the value came from `appsettings.json`, an environment file, or user-secrets.

User-secrets are explicitly not a production mechanism. The provider is not registered in non-development environments, and the storage location is the developer's user profile, not a server's filesystem. Production deployments use environment variables or a managed secret store (covered below).

## Environment variables and the override precedence

An **environment variable** is a key-value pair set in the operating-system process environment; ASP.NET Core reads them as a configuration provider, which is the standard mechanism for supplying production secrets and per-deployment settings. The provider sits later in the chain than the JSON files, so any environment variable overrides a value with the same key from any file.

Hierarchical keys translate using a double-underscore separator because most operating systems prohibit colons in environment variable names. The key `MongoDB:ConnectionString` becomes the variable name `MongoDB__ConnectionString`. The double underscore is recognized by the environment-variables provider and converted back into a colon when populating the configuration tree, so the consumer reading `configuration["MongoDB:ConnectionString"]` receives the value transparently.

### Worked example

Consider an `appsettings.json` that defines a default MongoDB connection string for local development:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017/cloudsoft",
    "DatabaseName": "cloudsoft"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

In development, this file alone is sufficient — the application connects to a MongoDB instance running locally on port 27017. In production, the same code must connect to a managed MongoDB cluster with credentials. Rather than editing the JSON file or adding an `appsettings.Production.json`, the deployment platform sets a single environment variable:

```bash
export MongoDB__ConnectionString="mongodb+srv://app:s3cret@cluster0.mongodb.net/cloudsoft"
```

When the application starts, the environment-variables provider sees `MongoDB__ConnectionString`, splits the double underscore into the path `MongoDB:ConnectionString`, and overrides the default value from `appsettings.json`. The compiled binary did not change, the JSON file did not change, but the runtime configuration tree now contains the production connection string. The companion exercise [data layer](/exercises/10-webapp-development/3-data-layer/) walks through this exact pattern when wiring a controller to a MongoDB repository.

Command-line arguments sit at the end of the chain and override even environment variables. Passing `--MongoDB:ConnectionString="mongodb://staging-host:27017/test"` to `dotnet run` replaces whatever the environment variable supplied, which is convenient for one-off diagnostic runs.

## Strongly-typed configuration with IOptions

Reading configuration through `configuration["MongoDB:ConnectionString"]` works but has two weaknesses. The key is a magic string — a typo compiles cleanly and fails only at runtime. The consumer must know the path of the section in the configuration tree, which couples it to the file layout. The **`IOptions<T>`** pattern fixes both issues by binding a section of configuration to a strongly-typed C# class and injecting it into services, decoupling the consumer from the configuration source and the path of the section in the underlying tree.

The pattern requires three pieces. A C# class with properties matching the configuration keys, a registration call in `Program.cs` that binds a configuration section to that class, and a constructor parameter of type `IOptions<T>` in the consumer:

```csharp
public class MongoOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}

// Program.cs
builder.Services.Configure<MongoOptions>(
    builder.Configuration.GetSection("MongoDB"));

// Repository
public class SubscriberRepository : ISubscriberRepository
{
    private readonly MongoOptions _options;

    public SubscriberRepository(IOptions<MongoOptions> options)
    {
        _options = options.Value;
    }
}
```

The `Configure<MongoOptions>` call tells the dependency injection container to populate a `MongoOptions` instance from the `MongoDB` section of `IConfiguration` whenever a service requests `IOptions<MongoOptions>`. The repository receives the bound object and reads `_options.ConnectionString` as a typed property — a typo on the property name is a compile error, and the section path lives in exactly one place. Refactoring the file structure (renaming the `MongoDB` section to `Mongo`) requires changing one registration line, not every consumer.

`IOptions<T>` is a snapshot taken once at registration time and is safe for singleton services. Two related abstractions exist for cases where configuration may change at runtime: `IOptionsSnapshot<T>` re-binds per request, and `IOptionsMonitor<T>` notifies subscribers of changes. The basic `IOptions<T>` is the right default for connection strings and other settings that do not change after startup.

## The boundary between development and production secret stores

User-secrets and environment variables both keep credentials out of source control, but they differ in operational scope. User-secrets store values in a developer's home directory, accessible only to that user account on that machine. Environment variables on a production host are visible to anyone with shell access to the host, and platform configuration UIs that set them are typically protected by the same access controls as the deployment itself.

Neither mechanism is sufficient for production-grade secret management. Both store secrets in plaintext at rest, neither rotates them, and neither audits access. Production deployments use a dedicated secret store — Azure Key Vault, AWS Secrets Manager, HashiCorp Vault — that encrypts secrets at rest, controls access through identity-based policies, logs every read, and supports rotation without redeployment. The application receives a managed identity from the hosting platform, and the secret store grants that identity permission to read specific secrets. The full mechanism, including managed identities and Key Vault integration, is covered in Part V.

The configuration provider chain already accommodates this. A Key Vault configuration provider can be added to the chain alongside the JSON and environment-variables providers, and the secrets it loads appear as ordinary `IConfiguration` keys. Application code that reads `configuration["MongoDB:ConnectionString"]` does not change when the source migrates from an environment variable to Key Vault — the abstraction is the same. The boundary is therefore an operational one, not an application-code one: development uses user-secrets, staging and production use the platform's secret store, and the consuming code is identical across all three.

| Provider | Scope | When to use | Lives where |
|----------|-------|-------------|-------------|
| `appsettings.json` | All environments | Non-secret defaults | Source control |
| `appsettings.{Env}.json` | Specific environment | Non-secret per-env overrides | Source control |
| User-secrets | Development only | Local credentials, API keys | Developer home directory |
| Environment variables | Any environment | Per-deployment settings, production secrets | Process environment |
| Secret store (Part V) | Staging, Production | Production-grade secret management | Cloud-managed vault |

## Summary

Configuration in ASP.NET Core is loaded by a chain of providers — `appsettings.json`, `appsettings.{Environment}.json`, user-secrets, environment variables, and command-line arguments — that merge into a single hierarchical key-value tree exposed through `IConfiguration`. The `ASPNETCORE_ENVIRONMENT` variable selects which environment-specific JSON file is layered on top of the base file and gates developer-only features such as the user-secrets provider and the developer exception page. Hierarchical keys map between providers using a colon in JSON and a double underscore in environment variable names, so `MongoDB:ConnectionString` and `MongoDB__ConnectionString` address the same value. The `IOptions<T>` pattern binds a configuration section to a strongly-typed class, removing magic strings from consumer code and decoupling consumers from the source and path of their settings. User-secrets and environment variables keep credentials out of source control during development and routine deployment, but production-grade secret management — encryption at rest, access auditing, rotation — requires a managed secret store layered into the same provider chain, covered in Part V.
