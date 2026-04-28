# Part III — Application Development — Mining Notes

## Studieguide alignment

- Companion course weeks: BCD week 3 (v.7) "Web Development" and BCD week 5 (v.9) "Web Development fördjupning"
- Reflection questions across these weeks (extract verbatim):
  - Vad är MVC-arkitektur och varför används det?
  - Hur fungerar HTTP-protokollet?
  - Vad är skillnaden mellan GET och POST?
  - Vad är en 3-tier arkitektur?
  - Hur hanteras konfiguration i olika miljöer?
  - Varför separerar man presentation, logik och data?

## Companion exercise

- Path: `content/exercises/10-webapp-development/`
- 5 sub-exercises:
  - 1-presentation-layer: Building the user interface layer of an ASP.NET Core MVC web application, covering views, controllers, forms, validation, and styling.
  - 2-service-layer: Implementing business logic with dependency injection and async patterns using service interfaces and the Result pattern.
  - 3-data-layer: Implementing data access with the repository pattern and cloud storage (MongoDB, Azure Blob Storage).
  - 4-authentication-authorization: Cookie authentication, roles, claims, policies, and CSRF protection in ASP.NET Core without ASP.NET Core Identity (theory belongs to Part V).
  - 5-identity-and-user-stores: ASP.NET Core Identity integration, user persistence, password hashing, role management, and registration flows (theory belongs to Part V).
- Key code patterns mentioned: Controllers, Views, Models, IServiceCollection, INewsletterService, OperationResult pattern, ISubscriberRepository, ConcurrentDictionary, async/await, Task<T>, dependency injection, interfaces, SOLID principles.
- Key file names: Controllers/, Views/, Models/, Services/, Repositories/, Program.cs, .cshtml, appsettings.json.
- Key library / API surface: ASP.NET Core MVC, IEnumerable<T>, async/await, Task.Run(), Task.FromResult(), StringComparer.OrdinalIgnoreCase, [Authorize] attribute, ClaimsPrincipal.

## Per-chapter brief

### Chapter 1 — HTTP fundamentals (slug: 1-http-fundamentals)
- Owns terms: HTTP, request, response, method, status code, header, URI, MIME type, GET, POST, query parameter, request body, response body.
- Borrows terms: TCP/IP (from Part II), client-server (from Part II), network layer (from Part II).
- Reflection questions to answer: How does the HTTP protocol work? What is the difference between GET and POST?
- Worked example to mine from exercise: "Hello World MVC Application" exercise demonstrates the basic HTTP request/response cycle — when a user navigates to `https://localhost:7240`, the browser sends an HTTP GET request to the ASP.NET Core server, which responds with HTML containing the welcome page. Exploring the default routes shows how GET requests map to controller actions returning IActionResult (specifically the default Home controller).
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/10-webapp-development/1-presentation-layer/
- Companion section in Part II: /course-book/2-infrastructure/network/

### Chapter 2 — The .NET platform (slug: 2-the-dotnet-platform)
- Owns terms: .NET Core, CLR (Common Language Runtime), IL (Intermediate Language), NuGet, assembly, runtime, framework, Razor, ASP.NET Core.
- Borrows terms: HTTP (from Chapter 1), client-server (from Part II).
- Reflection questions: What is .NET Core and how does it differ from .NET Framework? How does the ASP.NET Core platform enable web development?
- Worked example: The "Hello World MVC Application" exercise uses `dotnet new mvc -n CloudSoft` to scaffold a complete ASP.NET Core MVC project. The generated structure includes Controllers/, Views/, Models/, wwwroot/, and Program.cs. The exercise shows running `dotnet run` and navigating to localhost:7240, demonstrating the .NET Core runtime executing the compiled IL and serving HTTP responses.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/10-webapp-development/1-presentation-layer/

### Chapter 3 — The MVC pattern (slug: 3-the-mvc-pattern)
- Owns terms: MVC, controller, view, model, routing, model binding, action, ViewModel, Razor view engine, action method, action result.
- Borrows terms: HTTP method (from Chapter 1), URI (from Chapter 1), dependency injection (from Chapter 6).
- Reflection questions: What is MVC architecture and why is it used? How do controllers, views, and models work together?
- Worked example: The "Hello World" exercise creates a default MVC project structure. The HomeController (Controllers/) contains action methods like `public IActionResult Index()` that handle HTTP requests. The Index view (Views/Home/Index.cshtml) renders HTML using Razor syntax. Form exercises in the presentation layer (e.g., "Create Form Basic HTML") show model binding — a form POST sends data that the controller action receives as a strongly-typed parameter, ASP.NET Core automatically binds the form fields to model properties.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/10-webapp-development/1-presentation-layer/

### Chapter 4 — Three-tier architecture (slug: 4-three-tier-architecture)
- Owns terms: presentation layer, service layer, data layer, separation of concerns, abstraction, layer boundary, cross-layer communication.
- Borrows: dependency injection (from Chapter 6).
- Reflection questions: What is a 3-tier architecture? Why do we separate presentation, logic, and data? How do layers communicate?
- Worked example: Exercise 2 (Service Layer) shows refactoring from monolithic controllers into a three-layer structure. The presentation layer (Controllers/) calls INewsletterService methods; the service layer (Services/NewsletterService) contains business logic (validation, duplicate checking); the data layer (Repositories/InMemorySubscriberRepository) handles storage using ConcurrentDictionary. Each layer has a clear responsibility. The service layer depends on the repository interface, not the concrete implementation, enforcing loose coupling.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/10-webapp-development/2-service-layer/

### Chapter 5 — Configuration and environments (slug: 5-configuration-and-environments)
- Owns terms: IConfiguration, appsettings.json, appsettings.Development.json, appsettings.Production.json, environment variable, user-secrets, IOptions<T>, configuration provider, host environment.
- Borrows: dependency injection (from Chapter 6), three-tier architecture (from Chapter 4).
- Reflection questions: How is configuration handled in different environments? What is the difference between appsettings.json and user-secrets? When should you use environment variables?
- Worked example: ASP.NET Core applications include appsettings.json at the root. Environment-specific files like appsettings.Development.json override settings for development. The exercises show connection strings and credentials moving from hardcoded values in code to environment-specific configuration. IConfiguration is injected into controllers or services, allowing code to read config values without knowing the source. User-secrets are used during development to store sensitive values (API keys, database passwords) outside of version control.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/10-webapp-development/2-service-layer/ or /3-data-layer/

### Chapter 6 — Dependency injection (slug: 6-dependency-injection)
- Owns terms: dependency injection, DI container, IServiceCollection, IServiceProvider, lifetime (Singleton, Scoped, Transient), constructor injection, service registration, service resolution, interface, abstraction.
- Borrows: separation of concerns (from Chapter 4).
- Reflection questions: What is dependency injection and why is it important? What are the differences between Singleton, Scoped, and Transient lifetimes? How do you register and use dependencies?
- Worked example: Exercise 2 (Service Layer) shows DI in action. In Program.cs, the DI container is configured: `services.AddScoped<INewsletterService, NewsletterService>()` registers the service interface with its implementation. The presentation layer (NewsletterController) declares `public NewsletterController(INewsletterService service)` — the constructor parameter signals to the DI container that this controller depends on INewsletterService. At runtime, ASP.NET Core resolves the dependency and injects the NewsletterService instance. This decouples the controller from the concrete implementation, enabling easy testing and maintenance.
- Slide-pair: yes
- Course tag: BCD
- Cross-link target: /exercises/10-webapp-development/2-service-layer/

## Cross-Part dependencies (forward references)

- Authentication and authorization concepts are introduced briefly in presentation layer and MVC chapters (e.g., the [Authorize] attribute and ClaimsPrincipal abstraction used in Exercise 4) but core theory and ASP.NET Core Identity persistence are defined in Part V.
- Data persistence concepts (Entity Framework Core, DbContext, migrations, concrete repository implementations with SQL databases) are introduced briefly in data layer and three-tier chapters but full implementation details and database design are defined in Part IV.
- Advanced configuration patterns (Azure Key Vault integration, managed identities, secret rotation) are referenced in configuration chapter but detailed cloud security theory belongs to Part VI or beyond.

## Tonal reference

Use `content/course-book/2-infrastructure/network/2-ip-addresses-and-cidr-ranges/ip-addresses-and-cidr-ranges.md` as the gold standard. Key features to emulate:
- **Motivation paragraph** opens before any definition — e.g., the IP Addresses chapter opens with "Network communication requires a system for uniquely identifying devices and organizing them into manageable groups" rather than jumping to IPv4 specs.
- **Bold on first use** of every key term — "An **IP address** (Internet Protocol address) uniquely identifies..." and "The **/24** indicates that the first 24 bits..."
- **Worked examples** with one paragraph of interpretation after the code/CLI — the hello-world exercise shows `dotnet new mvc -n CloudSoft`, then explains what the command does and what it creates.
- **Closing Summary section** recapping load-bearing claims — the IP chapter ends with a paragraph tying together addresses, CIDR, subnets, NICs, routers, and DHCP as an integrated system.
- **1500–3500 words** per chapter — concise but detailed enough to convey both concepts and practical understanding.
