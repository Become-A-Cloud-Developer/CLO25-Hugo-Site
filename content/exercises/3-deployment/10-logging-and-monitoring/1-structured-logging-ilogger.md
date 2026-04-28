+++
title = "Structured Logging with ILogger<T>"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Replace ad-hoc Console.WriteLine logging with ASP.NET Core's ILogger<T>. Use semantic message templates so log entries carry structured fields, not just formatted strings. Tune log levels per category through appsettings.json. Observe the structured output locally with dotnet run and inside the container with docker run."
weight = 1
draft = false
+++

# Structured Logging with ILogger&lt;T&gt;

## Goal

Your `CloudCi` MVC app already runs end-to-end: a push to `main` builds an image, ships it to ACR, and rolls a new revision into Azure Container Apps. The homepage renders two badges — `build: <SHA>` and `host: <hostname>` — that prove the right image is serving. What it does *not* do is tell you anything about what happens between requests.

Right now the only log output is whatever ASP.NET Core happens to emit on its own: HTTP request lines from the hosting layer, a banner at startup, the occasional warning when a configuration value is missing. None of it is yours. None of it carries application meaning. If a user reports "the page rendered without my hostname yesterday afternoon," there is no log line to look for, because no log line was ever written.

In this exercise you will make the logging *deliberate*. You will inject `ILogger<HomeController>`, write log lines with **semantic message templates** (placeholders, not interpolation), and tune which lines are emitted through `appsettings.json`. You will run the app under `dotnet run` and inside `docker run` and confirm that the same structured fields flow through both.

> **What you'll learn:**
>
> - How `ILogger<T>` is registered and injected by the ASP.NET Core hosting model
> - Why message templates (`"... {Field} ..."`) preserve structure that string interpolation throws away
> - How log-level filtering works per category, and how `appsettings.json` controls it
> - How the simple and JSON console formatters differ in their *output*, while preserving the same *fields*
> - How container logs surface the exact same structured data as `dotnet run`

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ The `CloudCi` MVC app from the previous chapter, deployed to Azure Container Apps via the OIDC pipeline
> - ✓ The homepage renders two badges — `build: <SHA>` and `host: <hostname>` — both locally and in Azure
> - ✓ .NET 10 SDK and Docker Desktop (or another OCI runtime) installed locally
> - ✓ A terminal in the `CloudCi/` project root, with `git status` clean
> - ✓ No outstanding work in your `Dockerfile` or `Program.cs` from the last exercise

## Exercise Steps

### Overview

1. **Open the project and confirm the starting state**
2. **Observe what ASP.NET Core logs by default**
3. **Inject ILogger&lt;HomeController&gt; into the controller**
4. **Add a structured Information log line in Index()**
5. **Add a Warning when BUILD_SHA is missing**
6. **Run locally and inspect the captured fields**
7. **Tune log levels per category via appsettings.json**
8. **Demonstrate category filtering by raising the threshold**
9. **Switch the console formatter to JSON in Development**
10. **Build the container and run it locally**
11. **Inspect the container's logs and reflect on formatters vs fields**
12. **Test Your Implementation**

### **Step 1:** Open the project and confirm the starting state

Before changing anything, prove that the app you finished the previous chapter with still runs. The exercise builds on that exact state — the same `Program.cs`, the same `HomeController.cs`, the same `_Layout.cshtml` with the two badges. If the homepage doesn't render the badges now, no amount of logging work will fix it.

1. **Navigate** to the project root:

    ```bash
    cd CloudCi
    ```

2. **Run** the app in the foreground:

    ```bash
    dotnet run
    ```

3. **Open** the URL the host prints (typically `<http://localhost:5000>` or a similar 5xxx port).

4. **Confirm** that the homepage renders two badges in the layout — `build: local` (or whatever the fallback is) and `host: <your-machine-name>`.

5. **Stop** the app with `Ctrl+C` once verified.

> ✓ **Quick check:** The homepage renders both badges. `git status` shows no pending changes.

### **Step 2:** Observe what ASP.NET Core logs by default

The framework already logs. The question is *what*. Knowing the baseline matters because everything you add in the next steps must add **application** signal, not duplicate the framework's own.

1. **Run** the app again and watch the terminal carefully:

    ```bash
    dotnet run
    ```

2. **Make** a single request from another terminal:

    ```bash
    curl -i http://localhost:5000/
    ```

3. **Read** the output in the first terminal. You will see lines like:

    ```text
    info: Microsoft.Hosting.Lifetime[14]
          Now listening on: http://localhost:5000
    info: Microsoft.Hosting.Lifetime[0]
          Application started. Press Ctrl+C to shut down.
    info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
          Request starting HTTP/1.1 GET http://localhost:5000/ - - -
    info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
          Request finished HTTP/1.1 GET http://localhost:5000/ - 200 - text/html;...
    ```

4. **Stop** the app with `Ctrl+C`.

> ℹ **Concept Deep Dive**
>
> Each line follows the same shape: `level: Category[EventId]` on one line, then the message indented underneath. The category is the fully-qualified type name of whoever called the logger. `Microsoft.Hosting.Lifetime` and `Microsoft.AspNetCore.Hosting.Diagnostics` are framework categories — you did not write any of those log lines. They come from the host and the request pipeline.
>
> What is **not** in this output: anything specific to your `HomeController`, anything mentioning the build SHA, anything you could grep for if a user reported a bug at 14:32 yesterday. The framework gives you protocol-level signal for free; the application-level signal is yours to add.
>
> ✓ **Quick check:** You see at least one `Microsoft.Hosting.Lifetime` line and one `Microsoft.AspNetCore.Hosting.Diagnostics` line per request, and zero lines with `CloudCi` in the category.

### **Step 3:** Inject ILogger&lt;HomeController&gt; into the controller

`ILogger<T>` is the canonical logging abstraction in ASP.NET Core. The hosting model registers it as a generic service: ask for `ILogger<HomeController>` in a constructor, and the DI container hands you a logger whose **category** is automatically `CloudCi.Controllers.HomeController` — exactly the string `appsettings.json` will use to filter levels later.

1. **Open** the existing controller at `Controllers/HomeController.cs`.

2. **Locate** the class declaration. The default `dotnet new mvc` template produces something close to this — your `Index()` already pulls `BUILD_SHA` and `HOSTNAME` from the environment to render the two badges, so keep that logic and just add a logger field and constructor parameter:

    > `Controllers/HomeController.cs` *(before)*

    ```csharp
    using Microsoft.AspNetCore.Mvc;

    namespace CloudCi.Controllers;

    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var buildSha = Environment.GetEnvironmentVariable("BUILD_SHA") ?? "local";
            var hostName = Environment.MachineName;
            ViewData["BuildSha"] = buildSha;
            ViewData["HostName"] = hostName;
            return View();
        }
    }
    ```

3. **Replace** the class with the version that takes an injected logger:

    > `Controllers/HomeController.cs` *(after)*

    ```csharp
    using Microsoft.AspNetCore.Mvc;

    namespace CloudCi.Controllers;

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var buildSha = Environment.GetEnvironmentVariable("BUILD_SHA") ?? "local";
            var hostName = Environment.MachineName;
            ViewData["BuildSha"] = buildSha;
            ViewData["HostName"] = hostName;
            return View();
        }
    }
    ```

4. **Save** the file. Do not run yet — the next step adds the actual log line.

> ℹ **Concept Deep Dive**
>
> No service registration is needed. The default ASP.NET Core host calls `AddLogging()` internally as part of `WebApplication.CreateBuilder(args)`, which registers the open generic `ILogger<>` against `Logger<>`. Every controller in your app can add an `ILogger<TController>` parameter to its constructor and receive a working logger immediately.
>
> The category embedded in `ILogger<HomeController>` is the string form of `typeof(HomeController).FullName` — i.e. `CloudCi.Controllers.HomeController`. That string is what `appsettings.json` matches against in Step 7. Categories are hierarchical: a filter on `CloudCi` covers everything in your project; a filter on `CloudCi.Controllers.HomeController` covers only this controller.
>
> ⚠ **Common Mistakes**
>
> - Declaring `_logger` but forgetting the constructor parameter, then trying to call `_logger.LogInformation(...)`, throws `NullReferenceException` at the first request.
> - Using `ILogger` (non-generic) works at runtime but loses the category — log lines come out under `Microsoft.Extensions.Logging.Logger`, which makes per-category filtering impossible.
> - Newing up `Logger<HomeController>` by hand defeats the DI system and produces a logger with no providers attached. Always inject.
>
> ✓ **Quick check:** `dotnet build` succeeds with no warnings about unused fields.

### **Step 4:** Add a structured Information log line in Index()

The first real log line. The whole point of structured logging is that this line should carry **named fields**, not a frozen string. A sink (Log Analytics, Application Insights, Seq, Elasticsearch) can later filter on `HostName == "abc-123"` or aggregate by `BuildSha`. That is only possible if the original log call expressed those values as separate placeholders.

1. **Open** `Controllers/HomeController.cs` again.

2. **Add** a log line at the top of `Index()`, before assigning to `ViewData`. The placeholders `{HostName}` and `{BuildSha}` are *names*, not C# variable references — the values come from the trailing arguments:

    > `Controllers/HomeController.cs`

    ```csharp
    public IActionResult Index()
    {
        var buildSha = Environment.GetEnvironmentVariable("BUILD_SHA") ?? "local";
        var hostName = Environment.MachineName;

        _logger.LogInformation(
            "Home page rendered for {HostName} build {BuildSha}",
            hostName,
            buildSha);

        ViewData["BuildSha"] = buildSha;
        ViewData["HostName"] = hostName;
        return View();
    }
    ```

3. **Save** the file.

> ℹ **Concept Deep Dive — what makes a log line "structured"**
>
> A traditional log line is a single string: `"Home page rendered for abc-123 build a1b2c3"`. Once written, the host name and the SHA are dissolved into one opaque blob. To answer "show me all renders for `abc-123`" downstream, you have to parse the message back apart with a regex.
>
> A *structured* log line carries the values separately:
>
> ```text
> Message: "Home page rendered for {HostName} build {BuildSha}"
> Fields:  HostName = "abc-123", BuildSha = "a1b2c3"
> ```
>
> The console formatter still prints `"Home page rendered for abc-123 build a1b2c3"` for human eyes, but the underlying `LogRecord` keeps `HostName` and `BuildSha` as discrete properties. A sink that understands structure (Log Analytics with the right pipeline, Application Insights, Serilog) stores them as columns. A sink that doesn't (a plain text file) flattens them — but the placeholders make re-extraction trivial.
>
> ⚠ **Common Mistakes**
>
> - Writing `_logger.LogInformation($"Home page rendered for {hostName} build {buildSha}")` is *string interpolation* — C# builds the final string at the call site, the placeholders never reach the logging pipeline, and the structure is destroyed before the log call begins. The compiler does not warn about this; the analyzer rule `CA2254` does, if enabled.
> - Inverting the placeholder order (`"... {BuildSha} ... {HostName}"`) without inverting the trailing arguments produces a silently wrong log line. Match positionally.
> - Using `string.Format(...)` and passing the result to `LogInformation` collapses the structure exactly like interpolation.
>
> ✓ **Quick check:** The placeholders use PascalCase names (`{HostName}`, `{BuildSha}`), and the trailing arguments line up with them positionally.

### **Step 5:** Add a Warning when BUILD_SHA is missing

A Warning is for an event that is not an error but is worth noticing. "We rendered the homepage with the literal string `local` because no build SHA was set" is a perfect example: the app works, but in production this almost certainly means the deployment forgot to inject the env var.

1. **Open** `Controllers/HomeController.cs`.

2. **Replace** the body of `Index()` with the version that distinguishes the missing-env-var case:

    > `Controllers/HomeController.cs`

    ```csharp
    public IActionResult Index()
    {
        var buildShaEnv = Environment.GetEnvironmentVariable("BUILD_SHA");
        var buildSha = buildShaEnv ?? "local";
        var hostName = Environment.MachineName;

        if (buildShaEnv is null)
        {
            _logger.LogWarning(
                "BUILD_SHA environment variable is not set; falling back to {Fallback}",
                buildSha);
        }

        _logger.LogInformation(
            "Home page rendered for {HostName} build {BuildSha}",
            hostName,
            buildSha);

        ViewData["BuildSha"] = buildSha;
        ViewData["HostName"] = hostName;
        return View();
    }
    ```

3. **Save** the file.

> ℹ **Concept Deep Dive — the log-level hierarchy**
>
> ASP.NET Core uses six levels, ordered by severity:
>
> - `Trace` — extremely detailed; almost never in production.
> - `Debug` — diagnostic detail useful while developing.
> - `Information` — normal application flow worth recording.
> - `Warning` — something unexpected, but the app is still functioning.
> - `Error` — a request or operation failed.
> - `Critical` — the application is in an unrecoverable state.
>
> A category's effective minimum level is the highest of: the framework default (`Information`), the value in `appsettings.json`, and the value in `appsettings.{Environment}.json`. Lines below the threshold are *cheap to ignore* — the logging infrastructure short-circuits before formatting placeholders, so a disabled `LogDebug` call costs roughly the price of a virtual call and a comparison. You can leave Debug lines in production code with no measurable cost.
>
> ✓ **Quick check:** The Warning is conditional on `buildShaEnv is null`; the Information line runs unconditionally.

### **Step 6:** Run locally and inspect the captured fields

Time to see the new log lines in action. The default console formatter prints them as plain text, but the structure is already there underneath — Step 9 will make the structure visible by switching formatters.

1. **Make sure** `BUILD_SHA` is *not* set in your shell. On macOS / Linux:

    ```bash
    unset BUILD_SHA
    ```

2. **Run** the app:

    ```bash
    dotnet run
    ```

3. **Hit** the homepage from another terminal:

    ```bash
    curl -s http://localhost:5000/ > /dev/null
    ```

4. **Read** the log output. Among the framework lines you will now see two new ones, both under the category `CloudCi.Controllers.HomeController`:

    ```text
    warn: CloudCi.Controllers.HomeController[0]
          BUILD_SHA environment variable is not set; falling back to local
    info: CloudCi.Controllers.HomeController[0]
          Home page rendered for your-machine-name build local
    ```

5. **Stop** the app with `Ctrl+C`.

6. **Set** `BUILD_SHA` and re-run, confirming the Warning disappears:

    ```bash
    BUILD_SHA=manual-test dotnet run
    ```

    Hit `/` once more. The Information line now reports `build manual-test`; no Warning is emitted.

7. **Stop** the app.

> ✓ **Quick check:** Without `BUILD_SHA`, you see one Warning and one Information per request from your category. With `BUILD_SHA` set, you see only the Information line.

### **Step 7:** Tune log levels per category via appsettings.json

Until now you have been at the mercy of the default level (`Information`). To control which categories speak and which stay silent, ASP.NET Core reads the `Logging:LogLevel` section of configuration. Adding a Debug line and a per-category override is the cleanest way to feel how the filter works.

1. **Open** `Controllers/HomeController.cs` and **add** a Debug line just before the Information line:

    > `Controllers/HomeController.cs`

    ```csharp
    _logger.LogDebug("Resolved hostName={HostName} buildSha={BuildSha}", hostName, buildSha);

    _logger.LogInformation(
        "Home page rendered for {HostName} build {BuildSha}",
        hostName,
        buildSha);
    ```

2. **Open** `appsettings.json` in the project root. The default file looks like this:

    > `appsettings.json` *(before)*

    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "AllowedHosts": "*"
    }
    ```

3. **Add** a category-specific entry for your controller:

    > `appsettings.json` *(after)*

    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning",
          "CloudCi.Controllers.HomeController": "Debug"
        }
      },
      "AllowedHosts": "*"
    }
    ```

4. **Run** the app and hit `/`:

    ```bash
    dotnet run
    ```

5. **Confirm** all three lines now appear for your category — the Debug, the Warning (if `BUILD_SHA` is unset), and the Information. The framework categories still emit only Warning and above, because their entry (`Microsoft.AspNetCore: Warning`) takes precedence over `Default: Information`.

6. **Stop** the app.

> ℹ **Concept Deep Dive — category filtering**
>
> The filter resolution walks **most-specific to least-specific**. For a log call from `CloudCi.Controllers.HomeController`, ASP.NET Core checks:
>
> 1. `CloudCi.Controllers.HomeController` — exact match
> 2. `CloudCi.Controllers` — prefix match
> 3. `CloudCi` — prefix match
> 4. `Default` — fallback
>
> The first match wins. This means a single line `"CloudCi": "Debug"` would turn on Debug for every category in your project; a single line `"CloudCi.Controllers.HomeController": "Warning"` mutes Information and Debug for that one type only.
>
> ⚠ **Common Mistakes**
>
> - Editing `appsettings.json` while the app is running has no effect — the default host **does** reload configuration, but the logger filter is cached at startup unless you opt into `IOptionsMonitor`. Restart the app to be sure.
> - Putting the override in `appsettings.Development.json` and then running the app with `ASPNETCORE_ENVIRONMENT=Production` (e.g. inside the container) means the override silently disappears. Step 11 returns to this point.
> - Using kebab-case (`cloud-ci.controllers...`) — category matching is case-insensitive but punctuation must match. Use the exact namespace.
>
> ✓ **Quick check:** Your category emits Debug, Warning, and Information. Framework categories still emit only Warning.

### **Step 8:** Demonstrate category filtering by raising the threshold

The fastest way to feel that the filter is doing real work is to crank it up to `Warning` and watch the Information and Debug lines disappear.

1. **Edit** `appsettings.json`:

    > `appsettings.json`

    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning",
          "CloudCi.Controllers.HomeController": "Warning"
        }
      },
      "AllowedHosts": "*"
    }
    ```

2. **Restart** the app:

    ```bash
    dotnet run
    ```

3. **Hit** `/` once. Confirm that:

    - the Debug line is gone,
    - the Information line is gone,
    - the Warning line still fires (if `BUILD_SHA` is unset).

4. **Stop** the app.

5. **Set** the override back to `Information` for the rest of the exercise — the demonstrations from this point on assume Information is on:

    > `appsettings.json`

    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning",
          "CloudCi.Controllers.HomeController": "Information"
        }
      },
      "AllowedHosts": "*"
    }
    ```

> ✓ **Quick check:** With the threshold at Warning, only Warnings (and Errors and Critical) flow. With it back at Information, the Information line returns; Debug stays muted.

### **Step 9:** Switch the console formatter to JSON in Development

The default console formatter prints lines for humans. The JSON formatter prints lines for machines — one JSON object per log entry, with the message template and the structured fields preserved as separate keys. This matters because the next exercise (the one that pivots to centralised logs) needs the fields, not the prose.

1. **Open** `appsettings.Development.json` (the file `dotnet new mvc` already created):

    > `appsettings.Development.json` *(before)*

    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      }
    }
    ```

2. **Add** a `Console` section that selects the JSON formatter:

    > `appsettings.Development.json` *(after)*

    ```json
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        },
        "Console": {
          "FormatterName": "json"
        }
      }
    }
    ```

3. **Run** the app and hit `/`:

    ```bash
    dotnet run
    ```

4. **Inspect** the new output. Each line is now a JSON object on its own line:

    ```json
    {"EventId":0,"LogLevel":"Information","Category":"CloudCi.Controllers.HomeController","Message":"Home page rendered for your-machine-name build local","State":{"Message":"Home page rendered for your-machine-name build local","HostName":"your-machine-name","BuildSha":"local","{OriginalFormat}":"Home page rendered for {HostName} build {BuildSha}"}}
    ```

   The `State` object is what makes this useful. `HostName` and `BuildSha` are real keys; `{OriginalFormat}` carries the message template; the rendered message is also there for human reading.

5. **Stop** the app.

> ℹ **Concept Deep Dive — why message templates beat string interpolation**
>
> The JSON above is the proof. If you had written `_logger.LogInformation($"Home page rendered for {hostName} build {buildSha}")`, the `State` object would contain only `Message` and `{OriginalFormat}` — both equal to the already-formatted string. There would be no `HostName` key, no `BuildSha` key. A downstream sink looking for "all renders where `HostName == 'abc-123'`" would have to regex it back out of the message.
>
> Message templates promise the sink that the same call site will always emit the same fields with the same names. That promise is what makes a log message *queryable* instead of *searchable*.
>
> ✓ **Quick check:** The console emits one JSON object per log call. The object contains a `State` block with `HostName` and `BuildSha` as discrete keys.

### **Step 10:** Build the container and run it locally

Now run the same code through the container build that ships to Azure. The point is to confirm that the structured log lines appear in `docker logs` exactly as they do under `dotnet run` — this is the bridge to centralised logging in the next exercise.

1. **Build** the image. The `BUILD_SHA` build arg is the same one your existing `Dockerfile` (from the previous chapter) bakes into the image:

    ```bash
    docker build \
      --build-arg BUILD_SHA=local-dev \
      -t cloudci:local .
    ```

2. **Run** the container, mapping its 8080 port to your host:

    ```bash
    docker run --rm -p 8080:8080 --name cloudci-test cloudci:local
    ```

3. **From another terminal**, hit the homepage:

    ```bash
    curl -s http://localhost:8080/ > /dev/null
    ```

4. **Watch** the first terminal. You should see:

    - the framework's own Information lines about request start/finish,
    - your `CloudCi.Controllers.HomeController` Information line (`build local-dev`).

   You will *not* see the Warning, because `BUILD_SHA` is set (to `local-dev`) inside the image at build time.

5. **Stop** the container with `Ctrl+C`.

> ⚠ **Common Mistakes**
>
> - Forgetting `--build-arg BUILD_SHA=...` produces an image whose `BUILD_SHA` is empty. The Warning fires on every request and the badge displays `local`. Useful for triggering the Warning path on purpose; surprising if you didn't mean to.
> - Mapping the wrong port. The Dockerfile from the previous chapter has the app listening on 8080 inside the container; if you used `-p 5000:8080` instead, hit `<http://localhost:5000>` accordingly.
> - Forgetting `--rm` leaves stopped containers piling up under `docker ps -a`. Harmless, but tidier to add it.
>
> ✓ **Quick check:** `docker run` produces the same `CloudCi.Controllers.HomeController` Information line as `dotnet run` did.

### **Step 11:** Inspect the container's logs and reflect on formatters vs fields

A subtle but important detail: inside the container, `ASPNETCORE_ENVIRONMENT` defaults to `Production`, so `appsettings.Development.json` is **not** loaded. Your JSON formatter override therefore does not take effect — the simple console formatter prints the lines as text. To prove that the *fields* are still there even when the *formatter* is plain, run the same image with the Development environment forced on.

1. **Run** the container as Production (the default). In one terminal:

    ```bash
    docker run --rm -d -p 8080:8080 --name cloudci-prod cloudci:local
    ```

2. **Hit** the page and dump the logs from a second terminal:

    ```bash
    curl -s http://localhost:8080/ > /dev/null
    docker logs cloudci-prod
    ```

   The Information line appears as plain text — no JSON, no structure visible to the eye.

3. **Stop** the container:

    ```bash
    docker stop cloudci-prod
    ```

4. **Run** it again, this time forcing Development so the JSON formatter kicks in:

    ```bash
    docker run --rm -d -p 8080:8080 \
      -e ASPNETCORE_ENVIRONMENT=Development \
      --name cloudci-dev cloudci:local
    ```

5. **Hit** the page and dump the logs:

    ```bash
    curl -s http://localhost:8080/ > /dev/null
    docker logs cloudci-dev
    ```

   The Information line is now a JSON object, with `HostName` and `BuildSha` as keys exactly as in Step 9.

6. **Stop** the container:

    ```bash
    docker stop cloudci-dev
    ```

> ℹ **Concept Deep Dive — formatters vs fields**
>
> The formatter decides what bytes show up on stdout. The fields are part of the `LogRecord` regardless. When you ship to a centralised sink that *parses* container stdout (Log Analytics, Loki, Fluent Bit), one of two things happens:
>
> - the sink already speaks JSON — every field becomes a column automatically;
> - the sink reads plain text — every field is still recoverable, but only if a parser at the sink rewrites the message back into structure (regex, Grok, ingestion-time KQL).
>
> JSON-on-stdout is the easy path; plain-text-on-stdout is the cheap path. Both are common in production. What matters is that you wrote the log call with placeholders, because that is what makes either path *possible*.
>
> ⚠ **Common Mistakes**
>
> - Assuming `appsettings.Development.json` applies inside the container. It does not, unless you set `ASPNETCORE_ENVIRONMENT=Development`.
> - Putting the JSON formatter into `appsettings.json` (production-eligible) without thinking through the implications. JSON-on-stdout is a deliberate operational choice — make it consciously per environment.
>
> ✓ **Quick check:** With `ASPNETCORE_ENVIRONMENT=Production` (the default), `docker logs` shows plain text. With `ASPNETCORE_ENVIRONMENT=Development`, `docker logs` shows JSON. The same fields are present in both — the formatter is the only difference.

### **Step 12:** Test Your Implementation

Walk through the full chain end-to-end and confirm every layer behaves as designed.

1. **Verify** the app runs locally and emits your structured Information line on every request:

    ```bash
    dotnet run
    ```

    In another terminal:

    ```bash
    curl -s http://localhost:5000/ > /dev/null
    ```

    Expected in the first terminal: one `info: CloudCi.Controllers.HomeController` line per request, mentioning both the host name and the build SHA.

2. **Stop** the app.

3. **Verify** category filtering. Set the category to `Warning` in `appsettings.json`, restart, hit `/`, and confirm the Information line is suppressed while a Warning (when `BUILD_SHA` is unset) still flows. Restore the override to `Information` afterwards.

4. **Verify** the JSON formatter under Development. Re-run `dotnet run`, hit `/`, and confirm one log line per request comes out as a JSON object with `HostName` and `BuildSha` as discrete keys.

5. **Verify** the container path:

    ```bash
    docker build --build-arg BUILD_SHA=local-dev -t cloudci:local .
    docker run --rm -d -p 8080:8080 --name cloudci-test cloudci:local
    curl -s http://localhost:8080/ > /dev/null
    docker logs cloudci-test
    docker stop cloudci-test
    ```

   Expected: the same `CloudCi.Controllers.HomeController` Information line, in plain text (Production), mentioning `build local-dev`.

6. **Verify** structure survives in the container. Repeat with `-e ASPNETCORE_ENVIRONMENT=Development` and confirm the line is JSON with `HostName` and `BuildSha` as keys.

> ✓ **Success indicators:**
>
> - Every page render produces an Information log line under the `CloudCi.Controllers.HomeController` category.
> - When `BUILD_SHA` is missing, a Warning is also emitted on the same render.
> - Setting the category override to `Warning` in `appsettings.json` silences Debug and Information for that category only; framework categories are unaffected.
> - The JSON formatter (in Development) emits one JSON object per log line, with `HostName` and `BuildSha` as discrete fields.
> - `docker logs` shows the same lines as `dotnet run` — plain text by default, JSON if Development is forced.
>
> ✓ **Final verification checklist:**
>
> - ☐ `HomeController` takes `ILogger<HomeController>` via constructor injection
> - ☐ The Information log call uses placeholders (`{HostName}`, `{BuildSha}`), not interpolation
> - ☐ A Warning fires when `BUILD_SHA` is unset
> - ☐ A Debug line exists and is gated by the per-category override
> - ☐ `appsettings.json` has a `CloudCi.Controllers.HomeController` entry under `Logging:LogLevel`
> - ☐ `appsettings.Development.json` selects the JSON console formatter
> - ☐ `dotnet run` and `docker run` both emit the new log lines
> - ☐ The structured fields are visible (as JSON keys) when the formatter is JSON

## Common Issues

> **If you encounter problems:**
>
> **`NullReferenceException` on `_logger.LogInformation(...)`:** You declared `_logger` but forgot to assign it in the constructor — the field is null. Confirm `HomeController` has the `ILogger<HomeController> logger` parameter and assigns `_logger = logger`.
>
> **No `CloudCi.Controllers.HomeController` lines in the output, only framework lines:** Either the controller's `Index()` is not actually being hit (check the URL and port), or the category is filtered below Information. Hit `/` directly with `curl`, then check the `Logging:LogLevel` section in the config that matches your environment.
>
> **The JSON formatter is configured but plain text still appears:** Almost always one of two things. (1) The JSON setting is in `appsettings.Development.json` but the running process is in Production — set `ASPNETCORE_ENVIRONMENT=Development` or move the setting. (2) You edited the file but did not restart the app — restart.
>
> **The Information message is one big string with no structured fields in the JSON:** You used string interpolation (`$"... {hostName} ..."`) instead of a message template (`"... {HostName} ..."`). Replace the `$` interpolation with placeholders and pass the values as trailing arguments.
>
> **Setting `LogLevel:CloudCi.Controllers.HomeController` to `Warning` does not silence the Information line:** You edited the wrong file. Check whether the running process loaded `appsettings.json` or `appsettings.Development.json` — the Development file overrides the base file when the environment is Development. Edit the one that applies, or edit both.
>
> **`docker logs` is empty:** The container is logging to a different stream, or it crashed. Run without `-d` so the logs come to your terminal directly, and confirm the app started.
>
> **`docker build` complains it cannot find the Dockerfile:** Run the build from the directory that contains the `Dockerfile` (the project root), or pass `-f path/to/Dockerfile` explicitly.
>
> **Still stuck?** Add `_logger.LogInformation("Hello from HomeController constructor");` to the constructor itself — if even that line is missing from the output, the controller isn't being instantiated and the problem is upstream of logging.

## Summary

You moved the app from "whatever the framework happens to log" to "exactly the events I care about, with exactly the fields a downstream tool will need." Specifically:

- ✓ `ILogger<HomeController>` is injected by the hosting model with no extra registration.
- ✓ Log calls use semantic message templates, so `HostName` and `BuildSha` are first-class fields, not characters in a string.
- ✓ Per-category filtering through `appsettings.json` lets you raise or lower the volume of any single type without touching others.
- ✓ The console formatter is a presentation choice; the structure exists regardless of formatter.
- ✓ Container stdout carries the same fields as local stdout — what changes is the formatter, and that is itself a configuration concern.

> **Key takeaway:** Structured logging is not a feature of `ILogger<T>` — it is a *discipline*. The discipline is: every log call uses a constant message template with named placeholders, and never string interpolation. Followed consistently, that one rule turns your logs from text you grep into data you query. Everything that follows in monitoring — centralised log stores, dashboards, alerts — depends on that data being structured at the source.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Read the [Microsoft.Extensions.Logging documentation](<https://learn.microsoft.com/aspnet/core/fundamentals/logging/>) to see the full provider model — Console, Debug, EventSource, EventLog, plus third-party sinks. The same `ILogger<T>` calls flow to all configured providers.
> - Add [Serilog](<https://serilog.net/>) as a provider. The mental model is identical (templates, placeholders, sinks), but Serilog ships richer enrichers (machine name, thread ID, correlation IDs) and a much larger set of sinks (Seq, Elasticsearch, Splunk, files with rolling).
> - Add `LoggerMessage`-source-generated logging (`[LoggerMessage(...)]`) for high-frequency log calls. The compiler emits an allocation-free wrapper, useful when a log statement runs on every request.
> - Configure a [scope](<https://learn.microsoft.com/dotnet/core/extensions/logging#log-scopes>) with `_logger.BeginScope(...)` around an HTTP request so every log line in that request automatically carries a correlation ID.
> - In the next exercise the Container App's stdout becomes queryable in a Log Analytics workspace — the structured fields you set up here are exactly what makes that query meaningful.

## Done!

The app is no longer logging by accident. Every line that comes out of your code is one you wrote on purpose, with named fields you can search on, at a level you can tune without redeploying. That is the foundation everything else in this chapter rests on.

In the next exercise the Container App's stdout becomes queryable in a Log Analytics workspace; the `HostName` and `BuildSha` fields you established here are what makes that query meaningful — without them, the centralised store would just hold a pile of free-text strings.
