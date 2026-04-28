# Part III — Glossary

Terminology contract for the six chapters of Part III — Application Development. Workers in B2 receive this verbatim and follow the rules in `develop-theory-chapter/GLOSSARY-PROTOCOL.md`.

## Terms owned by this Part

### HTTP
- **Owner chapter**: `1-http-fundamentals`
- **Canonical definition**: **HTTP** (Hypertext Transfer Protocol) is the request-response protocol that browsers and APIs use to exchange data over a TCP connection. A client sends an HTTP request specifying a method, URI, and optional body, and the server returns an HTTP response with a status code, headers, and optional body.
- **Used by chapters**: 1-http-fundamentals (owner), 2-the-dotnet-platform, 3-the-mvc-pattern

### Request
- **Owner chapter**: `1-http-fundamentals`
- **Canonical definition**: An **HTTP request** is a message sent by a client to a server, consisting of a method (such as `GET` or `POST`), a target URI, headers describing the request, and an optional body carrying data.
- **Used by chapters**: 1-http-fundamentals (owner), 3-the-mvc-pattern

### Response
- **Owner chapter**: `1-http-fundamentals`
- **Canonical definition**: An **HTTP response** is the message a server returns after handling a request, consisting of a status code, headers describing the response, and an optional body carrying the requested representation.
- **Used by chapters**: 1-http-fundamentals (owner), 3-the-mvc-pattern

### Method (HTTP)
- **Owner chapter**: `1-http-fundamentals`
- **Canonical definition**: An **HTTP method** specifies the action a client wants to perform on the resource identified by the URI; the most common are `GET` (retrieve), `POST` (create or submit), `PUT` (replace), `PATCH` (partial update), and `DELETE` (remove).
- **Used by chapters**: 1-http-fundamentals (owner), 3-the-mvc-pattern

### Status code
- **Owner chapter**: `1-http-fundamentals`
- **Canonical definition**: An **HTTP status code** is a three-digit number returned with every response that signals the outcome; the first digit groups outcomes into 1xx informational, 2xx success, 3xx redirection, 4xx client error, and 5xx server error.
- **Used by chapters**: 1-http-fundamentals (owner)

### URI
- **Owner chapter**: `1-http-fundamentals`
- **Canonical definition**: A **URI** (Uniform Resource Identifier) is the string a client uses to address a resource on a server, typically composed of a scheme, host, path, and optional query string and fragment.
- **Used by chapters**: 1-http-fundamentals (owner), 3-the-mvc-pattern

### .NET
- **Owner chapter**: `2-the-dotnet-platform`
- **Canonical definition**: **.NET** is the cross-platform development platform from Microsoft for building applications in C# (and other languages) that compile to a common Intermediate Language and run on the .NET runtime; the unified version replaces the older .NET Framework.
- **Used by chapters**: 2-the-dotnet-platform (owner), 3-the-mvc-pattern, 4-three-tier-architecture, 5-configuration-and-environments, 6-dependency-injection

### CLR
- **Owner chapter**: `2-the-dotnet-platform`
- **Canonical definition**: The **CLR** (Common Language Runtime) is the .NET execution environment that loads compiled assemblies, performs just-in-time compilation of Intermediate Language to machine code, and provides services such as garbage collection and exception handling.
- **Used by chapters**: 2-the-dotnet-platform (owner)

### Assembly
- **Owner chapter**: `2-the-dotnet-platform`
- **Canonical definition**: An **assembly** is a compiled .NET unit — typically a `.dll` file — containing Intermediate Language, type metadata, and resources, that the CLR loads at runtime.
- **Used by chapters**: 2-the-dotnet-platform (owner), 6-dependency-injection

### NuGet
- **Owner chapter**: `2-the-dotnet-platform`
- **Canonical definition**: **NuGet** is the package manager for .NET; published packages contain assemblies and metadata, and projects declare dependencies in their `.csproj` files for the SDK to restore.
- **Used by chapters**: 2-the-dotnet-platform (owner), 5-configuration-and-environments

### ASP.NET Core
- **Owner chapter**: `2-the-dotnet-platform`
- **Canonical definition**: **ASP.NET Core** is the .NET web framework that hosts HTTP applications using a configurable request-handling pipeline, with built-in support for the MVC pattern, dependency injection, configuration, and middleware.
- **Used by chapters**: 2-the-dotnet-platform (owner), 3-the-mvc-pattern, 4-three-tier-architecture, 5-configuration-and-environments, 6-dependency-injection

### MVC
- **Owner chapter**: `3-the-mvc-pattern`
- **Canonical definition**: **MVC** (Model-View-Controller) is an architectural pattern that separates application concerns into three roles: the **model** holds data and business state, the **view** renders that state to the user, and the **controller** receives user input, coordinates the model and view, and decides what to return.
- **Used by chapters**: 3-the-mvc-pattern (owner), 4-three-tier-architecture

### Controller
- **Owner chapter**: `3-the-mvc-pattern`
- **Canonical definition**: A **controller** in ASP.NET Core MVC is a class whose public methods (called **actions**) handle incoming HTTP requests; an action returns an `IActionResult` describing what the framework should send back, such as a rendered view, JSON, or a redirect.
- **Used by chapters**: 3-the-mvc-pattern (owner), 4-three-tier-architecture, 6-dependency-injection

### View
- **Owner chapter**: `3-the-mvc-pattern`
- **Canonical definition**: A **view** is a Razor template (`.cshtml`) that renders HTML by combining static markup with C# expressions; ASP.NET Core resolves view names to template files using conventions over the `Views/` folder.
- **Used by chapters**: 3-the-mvc-pattern (owner), 4-three-tier-architecture

### Routing
- **Owner chapter**: `3-the-mvc-pattern`
- **Canonical definition**: **Routing** is the ASP.NET Core mechanism that matches an incoming request URI to a controller action, using either conventional templates configured at startup or attribute routes declared on actions.
- **Used by chapters**: 3-the-mvc-pattern (owner)

### Model binding
- **Owner chapter**: `3-the-mvc-pattern`
- **Canonical definition**: **Model binding** is the ASP.NET Core process that converts incoming request data — route values, query strings, form fields, and JSON bodies — into the typed parameters of an action method.
- **Used by chapters**: 3-the-mvc-pattern (owner)

### Three-tier architecture
- **Owner chapter**: `4-three-tier-architecture`
- **Canonical definition**: A **three-tier architecture** organizes an application into a presentation layer (UI), a service layer (business logic), and a data layer (persistence), with each layer depending only on the layer beneath it through abstractions.
- **Used by chapters**: 4-three-tier-architecture (owner), 5-configuration-and-environments, 6-dependency-injection

### Presentation layer
- **Owner chapter**: `4-three-tier-architecture`
- **Canonical definition**: The **presentation layer** is the part of an application responsible for accepting user input and rendering output; in an ASP.NET Core MVC app, controllers and views form this layer.
- **Used by chapters**: 4-three-tier-architecture (owner), 6-dependency-injection

### Service layer
- **Owner chapter**: `4-three-tier-architecture`
- **Canonical definition**: The **service layer** is the part of an application that contains the business logic — validation, workflow, orchestration — and exposes operations to the presentation layer through service interfaces.
- **Used by chapters**: 4-three-tier-architecture (owner), 6-dependency-injection

### Data layer
- **Owner chapter**: `4-three-tier-architecture`
- **Canonical definition**: The **data layer** is the part of an application responsible for persistent storage and retrieval, exposed to the service layer through repository abstractions; concrete implementations may use a database, object storage, or an in-memory collection.
- **Used by chapters**: 4-three-tier-architecture (owner), 5-configuration-and-environments, 6-dependency-injection

### Separation of concerns
- **Owner chapter**: `4-three-tier-architecture`
- **Canonical definition**: **Separation of concerns** is the design principle that each module of an application should have one reason to change; layers, classes, and methods are scoped so that unrelated responsibilities never live together.
- **Used by chapters**: 4-three-tier-architecture (owner), 6-dependency-injection

### IConfiguration
- **Owner chapter**: `5-configuration-and-environments`
- **Canonical definition**: **`IConfiguration`** is the ASP.NET Core abstraction that reads settings from a chain of providers — `appsettings.json`, environment-specific overrides, environment variables, command-line arguments, and user-secrets — and exposes them as a hierarchical key-value tree.
- **Used by chapters**: 5-configuration-and-environments (owner), 6-dependency-injection

### Appsettings.json
- **Owner chapter**: `5-configuration-and-environments`
- **Canonical definition**: **`appsettings.json`** is the default configuration file shipped with an ASP.NET Core project; environment-specific files such as `appsettings.Development.json` and `appsettings.Production.json` layer on top of it and override individual keys when the matching environment is active.
- **Used by chapters**: 5-configuration-and-environments (owner)

### User-secrets
- **Owner chapter**: `5-configuration-and-environments`
- **Canonical definition**: **User-secrets** is a development-only configuration provider that stores sensitive values outside the project directory (in the user's profile) so credentials are not committed to source control; production environments rely on environment variables or a secret store instead.
- **Used by chapters**: 5-configuration-and-environments (owner)

### Environment variable
- **Owner chapter**: `5-configuration-and-environments`
- **Canonical definition**: An **environment variable** is a key-value pair set in the operating-system process environment; ASP.NET Core reads them as a configuration provider, which is the standard mechanism for supplying production secrets and per-deployment settings.
- **Used by chapters**: 5-configuration-and-environments (owner)

### IOptions
- **Owner chapter**: `5-configuration-and-environments`
- **Canonical definition**: **`IOptions<T>`** is the ASP.NET Core pattern for binding a section of configuration to a strongly-typed C# class and injecting it into services, decoupling the consumer from the configuration source and the path of the section in the underlying tree.
- **Used by chapters**: 5-configuration-and-environments (owner)

### Dependency injection
- **Owner chapter**: `6-dependency-injection`
- **Canonical definition**: **Dependency injection** (DI) is a design pattern in which a class declares the services it needs through constructor parameters and a runtime container supplies the concrete implementations, decoupling the class from the lifecycle and choice of those services.
- **Used by chapters**: 6-dependency-injection (owner), 3-the-mvc-pattern, 4-three-tier-architecture, 5-configuration-and-environments

### IServiceCollection
- **Owner chapter**: `6-dependency-injection`
- **Canonical definition**: **`IServiceCollection`** is the ASP.NET Core builder used at startup (in `Program.cs`) to register services with the dependency injection container; each call associates a service type with a concrete implementation and a lifetime.
- **Used by chapters**: 6-dependency-injection (owner)

### Lifetime (DI)
- **Owner chapter**: `6-dependency-injection`
- **Canonical definition**: A **service lifetime** controls how often the dependency injection container creates a new instance: **Singleton** lasts for the application lifetime, **Scoped** lasts for one request, and **Transient** creates a new instance every time the service is resolved.
- **Used by chapters**: 6-dependency-injection (owner)

### Constructor injection
- **Owner chapter**: `6-dependency-injection`
- **Canonical definition**: **Constructor injection** is the dependency-injection style in which a class declares its required services as constructor parameters; the container resolves and supplies them when it instantiates the class, making the dependencies explicit and immutable.
- **Used by chapters**: 6-dependency-injection (owner)

## Terms borrowed from earlier Parts

### Server (compute concept)
- **Defined in**: Part II — Infrastructure / Compute / `1-what-is-a-server`
- **Reference link**: `/course-book/2-infrastructure/compute/1-what-is-a-server/`

### Client–server model
- **Defined in**: Part II — Infrastructure / Compute / `1-what-is-a-server`
- **Reference link**: `/course-book/2-infrastructure/compute/1-what-is-a-server/`

### TCP / IP / network layer
- **Defined in**: Part II — Infrastructure / Network
- **Reference link**: `/course-book/2-infrastructure/network/`

### Public vs private IP / firewall (referenced when discussing where the server sits)
- **Defined in**: Part II — Infrastructure / Network / `4-firewalls`
- **Reference link**: `/course-book/2-infrastructure/network/4-firewalls/`

### Cloud-native development (referenced when motivating ASP.NET Core's design)
- **Defined in**: Part I — Cloud Foundations / `4-cloud-native-development`
- **Reference link**: `/course-book/1-cloud-foundations/4-cloud-native-development/`
