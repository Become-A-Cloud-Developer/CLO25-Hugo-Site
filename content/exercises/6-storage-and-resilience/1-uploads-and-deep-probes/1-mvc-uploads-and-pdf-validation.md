+++
title = "MVC Uploads, PDF Validation, and First Deploy"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Scaffold an ASP.NET Core MVC recruitment portal that lets applicants upload PDF CVs against hard-coded job postings, validate each upload with a magic-bytes check, and ship it to Azure Container Apps using the OIDC-federated pipeline pattern with Application Insights layered on top."
weight = 1
draft = false
+++

# MVC Uploads, PDF Validation, and First Deploy

## Goal

Stand up an anonymous recruitment portal called `CloudCiCareers.Web` and ship it to Azure Container Apps. The portal lists a handful of hard-coded job postings, lets applicants attach a PDF CV, validates each upload with a four-byte signature check, and exposes a recruiter view that lists every application with status updates, notes, deletion, and CV download. Storage in this exercise is intentionally fragile — an in-memory store and a folder on disk inside the container — because the whole point of the next exercise is to swap that out for managed state. You arrive with the previous chapter's REST and DTO discipline, the deployment chapter's OIDC-federated pipeline, and the logging chapter's `secretref:` pattern for Application Insights — none of those are re-taught here.

> **What you'll learn:**
>
> - How to wire an MVC recruitment flow with a multipart upload form end to end
> - Why a four-byte magic-bytes check is the load-bearing piece of file-type validation, and what it deliberately doesn't tell you
> - How to stream `IFormFile` content directly to a blob abstraction without buffering it again
> - How antiforgery interacts with `enctype="multipart/form-data"`
> - How to apply the OIDC-federated pipeline pattern to a brand-new repo, with a smoke target that actually exists today
> - The difference between an in-memory store that survives one revision and managed state that survives the next one — and why this exercise stops short of fixing it

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ .NET 10 SDK installed (`dotnet --version` reports a `10.*` version)
> - ✓ Docker Desktop running, and `docker --version` succeeds
> - ✓ The `az` CLI signed in (`az login`) and the `gh` CLI signed in (`gh auth status`)
> - ✓ Completed the deployment chapter — you've previously built a GitHub Actions pipeline that uses OIDC federation to push to ACR and update a Container App
> - ✓ Completed the REST API and DTOs chapter — you're fluent with Container Apps, ACR, federated subjects, and the `secretref:` pattern for App Insights
> - ✓ Permission to create Entra app registrations in your tenant and Owner (or User Access Administrator) on the subscription
> - ✓ A real PDF file on disk (any small one will do) — you'll upload it during local testing

## Exercise Steps

### Overview

1. **Scaffold the `CloudCiCareers.Web` MVC project**
2. **Define the `Job`, `Application`, and `ApplyForm` types**
3. **Seed the job catalog**
4. **Add the `IApplicationStore` and the in-memory implementation**
5. **Add the `IBlobService` abstraction and a local-disk implementation**
6. **Add the PDF magic-bytes validator**
7. **Wire dependency injection in `Program.cs`**
8. **Build the `JobsController` and the apply form**
9. **Build the `ApplicationsController` and the recruiter views**
10. **Run locally and exercise the full applicant-then-recruiter flow**
11. **Containerize, set up the OIDC-federated pipeline, and deploy**
12. **Add Application Insights via the `secretref:` pattern**
13. **Test Your Implementation**

### **Step 1:** Scaffold the `CloudCiCareers.Web` MVC project

Start from the standard MVC template. The default `dotnet new mvc` scaffold gives you Razor views, controllers, antiforgery wired in by default, and the static-files pipeline you'll need for CSS — exactly the right shape for a portal that's going to render HTML.

1. **Create** the project and verify it builds:

   ```bash
   dotnet new mvc -o CloudCiCareers.Web --framework net10.0
   cd CloudCiCareers.Web
   dotnet build
   dotnet run --launch-profile http
   ```

   The terminal should report `Now listening on: http://localhost:<port>`. Stop the server with `Ctrl+C` once you've seen it start.

2. **Delete** the demo `Privacy` action and view, plus the `Models/ErrorViewModel.cs` reference if you don't plan to use it. You'll replace `Index` with a job listing in a later step, so leave the controller file in place but plan to gut its body.

   ```bash
   rm Views/Home/Privacy.cshtml
   ```

   In `Controllers/HomeController.cs`, remove the `Privacy()` action method. Leave the `Index()` action — you'll repoint it at the jobs page in Step 8.

3. **Add** an `uploads/` directory to `.gitignore`. The local-file blob service writes here during development, and the contents are throwaway:

   > `.gitignore`

   ```text
   uploads/
   ```

> ⚠ **Common Mistakes**
>
> - Forgetting `--framework net10.0` and ending up on whatever the default is on your box. The Dockerfile pulls `mcr.microsoft.com/dotnet/sdk:10.0`; if your `.csproj` says `net8.0`, the publish step fails.
>
> ✓ **Quick check:** `CloudCiCareers.Web.csproj` exists, `dotnet build` returns no errors, and `dotnet run` serves the default home page.

### **Step 2:** Define the `Job`, `Application`, and `ApplyForm` types

Three small types capture the entire data model. `Job` is the read-only domain entity for a posting (you never create or edit jobs at runtime — they're hard-coded). `Application` is the entity an applicant creates by submitting the form, with status that the recruiter then mutates. `ApplyForm` is the wire-bound form type — the shape of the multipart request body. Same entity-vs-wire discipline as the previous chapter, just with form binding instead of JSON.

1. **Create** the `Models/Job.cs` file:

   > `Models/Job.cs`

   ```csharp
   namespace CloudCiCareers.Web.Models;

   public record Job(
       int Id,
       string Title,
       string Department,
       string Description,
       DateTimeOffset Posted);
   ```

2. **Create** `Models/Application.cs` with the entity and the status enum side by side:

   > `Models/Application.cs`

   ```csharp
   namespace CloudCiCareers.Web.Models;

   public class Application
   {
       public string Id { get; set; } = string.Empty;
       public int JobId { get; set; }
       public string ApplicantName { get; set; } = string.Empty;
       public string ApplicantEmail { get; set; } = string.Empty;
       public string CvBlobName { get; set; } = string.Empty;
       public DateTimeOffset SubmittedAt { get; set; }
       public ApplicationStatus Status { get; set; }
       public string? Notes { get; set; }
   }

   public enum ApplicationStatus
   {
       Submitted,
       UnderReview,
       Rejected,
       Hired,
   }
   ```

3. **Create** `Models/ApplyForm.cs` — the form-binding type with validation attributes:

   > `Models/ApplyForm.cs`

   ```csharp
   using System.ComponentModel.DataAnnotations;

   namespace CloudCiCareers.Web.Models;

   public class ApplyForm
   {
       [Required]
       [StringLength(100, MinimumLength = 1)]
       public string Name { get; set; } = string.Empty;

       [Required]
       [EmailAddress]
       [StringLength(200)]
       public string Email { get; set; } = string.Empty;
   }
   ```

> ℹ **Concept Deep Dive: form-binding types are wire types**
>
> `ApplyForm` is to multipart forms what a DTO is to JSON: the exact shape the request binder produces. The entity carries `Id`, `SubmittedAt`, `Status`, and `CvBlobName` — none of which a client should be able to set. Keeping them on the entity but off the form-binding type is what makes the compiler enforce the boundary. The uploaded `IFormFile` is bound separately as an action parameter so the validation attributes don't fight with the upload-stream lifetime.
>
> ✓ **Quick check:** `dotnet build` succeeds.

### **Step 3:** Seed the job catalog

The portal has no admin interface — jobs are baked in. A tiny interface plus a static implementation is enough, and it sets up the swap to a backing store later (a different course; not this one) without changing the controller.

1. **Create** the interface:

   > `Services/IJobCatalog.cs`

   ```csharp
   using CloudCiCareers.Web.Models;

   namespace CloudCiCareers.Web.Services;

   public interface IJobCatalog
   {
       IReadOnlyList<Job> GetAll();
       Job? GetById(int id);
   }
   ```

2. **Create** the static implementation:

   > `Services/StaticJobCatalog.cs`

   ```csharp
   using CloudCiCareers.Web.Models;

   namespace CloudCiCareers.Web.Services;

   public class StaticJobCatalog : IJobCatalog
   {
       private static readonly IReadOnlyList<Job> _jobs = new List<Job>
       {
           new(1, "Cloud Engineer", "Platform",
               "Design, deploy, and operate Azure infrastructure for the product platform.",
               DateTimeOffset.UtcNow.AddDays(-7)),
           new(2, "Backend Developer", "Engineering",
               "Build and own ASP.NET Core services that back the customer-facing apps.",
               DateTimeOffset.UtcNow.AddDays(-5)),
           new(3, "DevOps Specialist", "Platform",
               "Build and maintain CI/CD, observability, and incident-response tooling.",
               DateTimeOffset.UtcNow.AddDays(-3)),
           new(4, "Site Reliability Engineer", "Platform",
               "Define SLOs, run game days, and keep production boring.",
               DateTimeOffset.UtcNow.AddDays(-1)),
       };

       public IReadOnlyList<Job> GetAll() => _jobs;

       public Job? GetById(int id) => _jobs.FirstOrDefault(j => j.Id == id);
   }
   ```

> ✓ **Quick check:** Both files compile. `StaticJobCatalog` returns four jobs from `GetAll()`.

### **Step 4:** Add the `IApplicationStore` and the in-memory implementation

The recruiter side needs a place to read applications back, mutate their status, and delete them. A thread-safe `ConcurrentDictionary` does the job for one replica — same singleton-store pattern as the previous chapter's `IQuoteStore`.

1. **Create** the interface:

   > `Services/IApplicationStore.cs`

   ```csharp
   using CloudCiCareers.Web.Models;

   namespace CloudCiCareers.Web.Services;

   public interface IApplicationStore
   {
       IEnumerable<Application> GetAll();
       Application? GetById(string id);
       Application Create(Application application);
       bool UpdateStatus(string id, ApplicationStatus newStatus, string? notes);
       bool Delete(string id);
   }
   ```

2. **Create** the in-memory implementation:

   > `Services/InMemoryApplicationStore.cs`

   ```csharp
   using System.Collections.Concurrent;
   using CloudCiCareers.Web.Models;

   namespace CloudCiCareers.Web.Services;

   public class InMemoryApplicationStore : IApplicationStore
   {
       private readonly ConcurrentDictionary<string, Application> _applications = new();

       public IEnumerable<Application> GetAll() =>
           _applications.Values.OrderByDescending(a => a.SubmittedAt);

       public Application? GetById(string id) =>
           _applications.TryGetValue(id, out var application) ? application : null;

       public Application Create(Application application)
       {
           if (string.IsNullOrEmpty(application.Id))
           {
               application.Id = Guid.NewGuid().ToString("n");
           }
           if (application.SubmittedAt == default)
           {
               application.SubmittedAt = DateTimeOffset.UtcNow;
           }
           _applications[application.Id] = application;
           return application;
       }

       public bool UpdateStatus(string id, ApplicationStatus newStatus, string? notes)
       {
           if (!_applications.TryGetValue(id, out var application))
           {
               return false;
           }
           application.Status = newStatus;
           application.Notes = notes;
           return true;
       }

       public bool Delete(string id) => _applications.TryRemove(id, out _);
   }
   ```

> ℹ **Concept Deep Dive**
>
> The store survives every request because it's registered as `Singleton` (Step 7); it does **not** survive a Container App revision rollover, because every revision is a fresh process. That's the motivation for the next exercise.
>
> ⚠ **Common Mistakes**
>
> - Registering this as `Scoped`. Every request gets a fresh empty dictionary; the recruiter view always shows zero applications.
>
> ✓ **Quick check:** Both files compile.

### **Step 5:** Add the `IBlobService` abstraction and a local-disk implementation

The CV bytes go somewhere — for now, a folder on disk. Putting an interface in front of `File.WriteAllBytesAsync` is what lets the next exercise replace the implementation with `BlobContainerClient` calls without touching the controllers. One line in `Program.cs` swaps the registration; upstream keeps compiling.

1. **Create** the interface:

   > `Services/IBlobService.cs`

   ```csharp
   namespace CloudCiCareers.Web.Services;

   public interface IBlobService
   {
       Task UploadAsync(string name, Stream content, CancellationToken ct);
       Task<Stream> OpenReadAsync(string name, CancellationToken ct);
       Task DeleteAsync(string name, CancellationToken ct);
   }
   ```

2. **Create** the local-disk implementation:

   > `Services/LocalFileBlobService.cs`

   ```csharp
   namespace CloudCiCareers.Web.Services;

   public class LocalFileBlobService : IBlobService
   {
       private readonly string _root;

       public LocalFileBlobService(IWebHostEnvironment env)
       {
           _root = Path.Combine(env.ContentRootPath, "uploads");
           Directory.CreateDirectory(_root);
       }

       public async Task UploadAsync(string name, Stream content, CancellationToken ct)
       {
           var path = Path.Combine(_root, name);
           await using var file = File.Create(path);
           await content.CopyToAsync(file, ct);
       }

       public Task<Stream> OpenReadAsync(string name, CancellationToken ct)
       {
           var path = Path.Combine(_root, name);
           Stream stream = File.OpenRead(path);
           return Task.FromResult(stream);
       }

       public Task DeleteAsync(string name, CancellationToken ct)
       {
           var path = Path.Combine(_root, name);
           if (File.Exists(path))
           {
               File.Delete(path);
           }
           return Task.CompletedTask;
       }
   }
   ```

> ℹ **Concept Deep Dive**
>
> The interface speaks only in `Stream`s and string names — no filesystem, no Azure Blob, no specific SDK. That's the abstraction boundary. The next exercise registers a managed-identity-authenticated Azure Blob implementation against the same three methods, and the controllers don't notice.
>
> ✓ **Quick check:** Both files compile.

### **Step 6:** Add the PDF magic-bytes validator

Every PDF on Earth starts with the same four bytes: `%PDF` (`0x25 0x50 0x44 0x46`). Reading those four bytes and comparing tells you whether the file is structurally a PDF, regardless of what the filename says. This is the core of trustworthy file-type validation: don't believe the extension, don't believe the `Content-Type` header — read the bytes the file actually starts with.

1. **Create** the validator:

   > `Services/PdfValidation.cs`

   ```csharp
   namespace CloudCiCareers.Web.Services;

   public static class PdfValidation
   {
       // PDF files always start with the four bytes "%PDF" (0x25 0x50 0x44 0x46).
       private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46 };

       public static bool IsPdf(Stream s)
       {
           if (s is null || !s.CanRead)
           {
               return false;
           }

           Span<byte> header = stackalloc byte[4];
           var read = s.Read(header);

           // CRITICAL: rewind so the caller's upload pipeline reads from byte zero.
           if (s.CanSeek)
           {
               s.Position = 0;
           }

           if (read < 4)
           {
               return false;
           }

           return header.SequenceEqual(PdfSignature);
       }
   }
   ```

> ℹ **Concept Deep Dive: magic bytes, ISO 32000, and what this check does NOT do**
>
> The four-byte `%PDF` header is part of the PDF file format specification (ISO 32000); RFC 8118 registers `application/pdf` as the canonical media type. Every conforming PDF starts with these bytes — anything that doesn't is either truncated, corrupt, or pretending.
>
> What a magic-bytes check **does** buy you: confirmation that the bytes you received are structurally a PDF, not a renamed `.exe` or a text file given a `.pdf` extension. What it does **not** buy you: any guarantee that the PDF is *safe*. PDFs can carry JavaScript, embedded files, malformed cross-references that crash parsers, viewer-specific exploits. Defending against those is layered separately — antivirus scanning at rest (Microsoft Defender for Storage), sandboxed parsing, content stripping with PdfPig, file-size limits. Magic bytes is layer one; the other layers are real and outside this chapter's scope.
>
> ⚠ **Common Mistakes**
>
> - Forgetting `s.Position = 0` after the read. The four-byte header is consumed, the upload pipeline writes from byte four onwards, and the saved file fails to open in any PDF viewer.
> - Treating `Content-Type` from the browser as a security check. Browsers send what the OS guessed from the extension; both are attacker-controlled. Magic bytes is what makes the check trustworthy.
>
> ✓ **Quick check:** `dotnet build` succeeds.

### **Step 7:** Wire dependency injection in `Program.cs`

Three singletons plus the standard MVC pipeline. Same shape as the BCD webapp series.

1. **Replace** the contents of `Program.cs`:

   > `Program.cs`

   ```csharp
   using CloudCiCareers.Web.Services;

   var builder = WebApplication.CreateBuilder(args);

   builder.Services.AddControllersWithViews();

   // Singletons — one shared instance per process (per Container App replica).
   builder.Services.AddSingleton<IJobCatalog, StaticJobCatalog>();
   builder.Services.AddSingleton<IApplicationStore, InMemoryApplicationStore>();
   builder.Services.AddSingleton<IBlobService, LocalFileBlobService>();

   var app = builder.Build();

   if (!app.Environment.IsDevelopment())
   {
       app.UseExceptionHandler("/Home/Error");
   }
   app.UseStaticFiles();
   app.UseRouting();
   app.UseAuthorization();

   app.MapControllerRoute(
       name: "default",
       pattern: "{controller=Jobs}/{action=Index}/{id?}");

   app.Run();
   ```

> ℹ **Concept Deep Dive**
>
> The default route now points at `JobsController.Index` — the home page IS the jobs page, no extra hop. Singleton is correct for all three services since none depend on a per-request type like `DbContext`.
>
> ✓ **Quick check:** `dotnet build` succeeds.

### **Step 8:** Build the `JobsController` and the apply form

Two GETs and one POST: the home page lists jobs, `/Jobs/Apply/3` renders the form for a specific job, and submitting that form does the validation, the upload, and the redirect. The POST is where every interesting decision lives — antiforgery, model state, magic bytes, blob streaming.

1. **Create** the controller:

   > `Controllers/JobsController.cs`

   ```csharp
   using CloudCiCareers.Web.Models;
   using CloudCiCareers.Web.Services;
   using Microsoft.AspNetCore.Mvc;

   namespace CloudCiCareers.Web.Controllers;

   public class JobsController : Controller
   {
       private readonly IJobCatalog _catalog;
       private readonly IApplicationStore _store;
       private readonly IBlobService _blobs;

       public JobsController(
           IJobCatalog catalog,
           IApplicationStore store,
           IBlobService blobs)
       {
           _catalog = catalog;
           _store = store;
           _blobs = blobs;
       }

       public IActionResult Index()
       {
           return View(_catalog.GetAll());
       }

       public IActionResult Apply(int id)
       {
           var job = _catalog.GetById(id);
           if (job is null)
           {
               return NotFound();
           }
           ViewData["Job"] = job;
           return View(new ApplyForm());
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Apply(int id, ApplyForm form, IFormFile? cv,
           CancellationToken ct)
       {
           var job = _catalog.GetById(id);
           if (job is null)
           {
               return NotFound();
           }
           ViewData["Job"] = job;

           if (cv is null || cv.Length == 0)
           {
               ModelState.AddModelError(nameof(cv),
                   "Please attach your CV as a PDF file.");
           }
           else if (!PdfValidation.IsPdf(cv.OpenReadStream()))
           {
               ModelState.AddModelError(nameof(cv),
                   "The uploaded file is not a valid PDF document.");
           }

           if (!ModelState.IsValid)
           {
               return View(form);
           }

           var blobName = $"{Guid.NewGuid():n}.pdf";

           // Stream straight from the request body to the blob — don't re-buffer.
           await using (var upload = cv!.OpenReadStream())
           {
               await _blobs.UploadAsync(blobName, upload, ct);
           }

           var application = new Application
           {
               JobId = job.Id,
               ApplicantName = form.Name,
               ApplicantEmail = form.Email,
               CvBlobName = blobName,
               Status = ApplicationStatus.Submitted,
           };

           _store.Create(application);
           TempData["Thanks"] = "Thanks for applying! We'll be in touch.";

           return RedirectToAction("Details", "Applications",
               new { id = application.Id });
       }
   }
   ```

2. **Create** the home-page view at `Views/Jobs/Index.cshtml`:

   > `Views/Jobs/Index.cshtml`

   ```cshtml
   @model IEnumerable<CloudCiCareers.Web.Models.Job>
   @{ ViewData["Title"] = "Open positions"; }

   <h1>Open positions</h1>
   <p class="lead">We're hiring across Platform and Engineering.</p>

   <div class="row">
   @foreach (var job in Model)
   {
       <div class="col-md-6 mb-3">
           <div class="card h-100"><div class="card-body">
               <h5 class="card-title">@job.Title</h5>
               <h6 class="card-subtitle text-muted mb-2">@job.Department</h6>
               <p class="card-text">@job.Description</p>
               <a class="btn btn-primary" asp-action="Apply"
                  asp-route-id="@job.Id">Apply</a>
           </div></div>
       </div>
   }
   </div>
   ```

3. **Create** the apply-form view at `Views/Jobs/Apply.cshtml`:

   > `Views/Jobs/Apply.cshtml`

   ```cshtml
   @model CloudCiCareers.Web.Models.ApplyForm
   @{
       var job = (CloudCiCareers.Web.Models.Job)ViewData["Job"]!;
       ViewData["Title"] = $"Apply for {job.Title}";
   }

   <h1>Apply for @job.Title</h1>
   <p class="text-muted">@job.Department</p>

   <form asp-action="Apply" asp-route-id="@job.Id"
         method="post" enctype="multipart/form-data">
       @Html.AntiForgeryToken()
       <div asp-validation-summary="All" class="text-danger"></div>

       <div class="mb-3">
           <label asp-for="Name" class="form-label">Full name</label>
           <input asp-for="Name" class="form-control" />
           <span asp-validation-for="Name" class="text-danger"></span>
       </div>
       <div class="mb-3">
           <label asp-for="Email" class="form-label">Email</label>
           <input asp-for="Email" class="form-control" type="email" />
           <span asp-validation-for="Email" class="text-danger"></span>
       </div>
       <div class="mb-3">
           <label for="cv" class="form-label">CV (PDF)</label>
           <input id="cv" name="cv" type="file" accept="application/pdf"
                  class="form-control" />
       </div>

       <button type="submit" class="btn btn-primary">Submit application</button>
   </form>
   ```

> ℹ **Concept Deep Dive: antiforgery on multipart forms**
>
> Antiforgery protects every state-changing request — without it, a malicious page on another origin could submit your form using the user's logged-in cookie. The mechanism: a server-stamped hidden form field plus a matching cookie, both validated by `[ValidateAntiForgeryToken]`. A cross-origin form can't read the cookie, so the replay fails. `@Html.AntiForgeryToken()` writes the field; the attribute validates it.
>
> The multipart caveat: `enctype="multipart/form-data"` does **not** break antiforgery — the token field travels in the multipart envelope just like any other form field. What it *does* mean is that you can't use `[FromBody]` (that's for JSON), so each field is bound by name from the multipart parts.
>
> ⚠ **Common Mistakes: stream the upload, don't buffer it**
>
> - Reading the upload into a `MemoryStream` first, then passing that to `IBlobService.UploadAsync`. Don't. `IFormFile.OpenReadStream()` is already buffered to disk by the request body reader once the body crosses 64 KB; re-buffering through a `MemoryStream` wastes process memory for a copy you don't need. Pipe the stream straight in.
> - Forgetting `enctype="multipart/form-data"` on the `<form>`. The browser then sends `application/x-www-form-urlencoded`, the file is omitted, and `cv` arrives as `null`.
> - Forgetting `[ValidateAntiForgeryToken]`. The submit appears to work in dev; the moment another origin can reach the form, it's exploitable.
>
> ✓ **Quick check:** `dotnet build` succeeds.

### **Step 9:** Build the `ApplicationsController` and the recruiter views

A list, a detail page with status + delete forms, and a CV-streaming action that proxies bytes from the blob service. `Content-Disposition: inline` makes the browser render the PDF in-tab — easier to verify visually that the upload round-tripped.

1. **Create** the controller:

   > `Controllers/ApplicationsController.cs`

   ```csharp
   using CloudCiCareers.Web.Models;
   using CloudCiCareers.Web.Services;
   using Microsoft.AspNetCore.Mvc;

   namespace CloudCiCareers.Web.Controllers;

   public class ApplicationsController : Controller
   {
       private readonly IApplicationStore _store;
       private readonly IJobCatalog _catalog;
       private readonly IBlobService _blobs;

       public ApplicationsController(
           IApplicationStore store,
           IJobCatalog catalog,
           IBlobService blobs)
       {
           _store = store;
           _catalog = catalog;
           _blobs = blobs;
       }

       public IActionResult Index()
       {
           ViewData["Catalog"] = _catalog;
           return View(_store.GetAll().ToList());
       }

       public IActionResult Details(string id)
       {
           var application = _store.GetById(id);
           if (application is null)
           {
               return NotFound();
           }
           ViewData["Job"] = _catalog.GetById(application.JobId);
           return View(application);
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public IActionResult UpdateStatus(string id,
           ApplicationStatus newStatus, string? notes)
       {
           if (!_store.UpdateStatus(id, newStatus, notes))
           {
               return NotFound();
           }
           return RedirectToAction(nameof(Details), new { id });
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public IActionResult Delete(string id)
       {
           if (!_store.Delete(id))
           {
               return NotFound();
           }
           return RedirectToAction(nameof(Index));
       }

       public async Task<IActionResult> Cv(string id, CancellationToken ct)
       {
           var application = _store.GetById(id);
           if (application is null)
           {
               return NotFound();
           }
           var stream = await _blobs.OpenReadAsync(application.CvBlobName, ct);
           Response.Headers["Content-Disposition"] = "inline; filename=\"cv.pdf\"";
           return File(stream, "application/pdf");
       }
   }
   ```

2. **Create** the recruiter list view at `Views/Applications/Index.cshtml`:

   > `Views/Applications/Index.cshtml`

   ```cshtml
   @model List<CloudCiCareers.Web.Models.Application>
   @{
       ViewData["Title"] = "Applications";
       var catalog = (CloudCiCareers.Web.Services.IJobCatalog)ViewData["Catalog"]!;
   }

   <h1>Applications</h1>

   @if (!Model.Any())
   {
       <p class="text-muted">No applications yet.</p>
   }
   else
   {
       <table class="table">
           <thead><tr>
               <th>Submitted</th><th>Job</th><th>Applicant</th>
               <th>Status</th><th></th>
           </tr></thead>
           <tbody>
           @foreach (var a in Model)
           {
               var job = catalog.GetById(a.JobId);
               <tr>
                   <td>@a.SubmittedAt.LocalDateTime</td>
                   <td>@(job?.Title ?? "(unknown)")</td>
                   <td>@a.ApplicantName<br/><small class="text-muted">@a.ApplicantEmail</small></td>
                   <td><span class="badge bg-secondary">@a.Status</span></td>
                   <td><a class="btn btn-sm btn-outline-primary"
                          asp-action="Details" asp-route-id="@a.Id">Open</a></td>
               </tr>
           }
           </tbody>
       </table>
   }
   ```

3. **Create** the detail view at `Views/Applications/Details.cshtml`:

   > `Views/Applications/Details.cshtml`

   ```cshtml
   @model CloudCiCareers.Web.Models.Application
   @using CloudCiCareers.Web.Models
   @{
       var job = (Job?)ViewData["Job"];
       ViewData["Title"] = $"Application — {Model.ApplicantName}";
   }

   @if (TempData["Thanks"] is string thanks)
   {
       <div class="alert alert-success">@thanks</div>
   }

   <h1>@Model.ApplicantName</h1>
   <p class="text-muted">
       Applied for <strong>@(job?.Title ?? "(unknown)")</strong>
       on @Model.SubmittedAt.LocalDateTime
   </p>
   <p><a asp-action="Cv" asp-route-id="@Model.Id" target="_blank">View CV (PDF)</a></p>

   <h2>Update status</h2>
   <form asp-action="UpdateStatus" asp-route-id="@Model.Id" method="post">
       @Html.AntiForgeryToken()
       <div class="mb-3">
           <label class="form-label">Status</label>
           <select name="newStatus" class="form-select">
           @foreach (var value in Enum.GetValues<ApplicationStatus>())
           {
               <option value="@value" selected="@(value == Model.Status)">@value</option>
           }
           </select>
       </div>
       <div class="mb-3">
           <label class="form-label">Notes</label>
           <textarea name="notes" class="form-control" rows="3">@Model.Notes</textarea>
       </div>
       <button type="submit" class="btn btn-primary">Save</button>
   </form>

   <hr/>

   <form asp-action="Delete" asp-route-id="@Model.Id" method="post"
         onsubmit="return confirm('Delete this application?');">
       @Html.AntiForgeryToken()
       <button type="submit" class="btn btn-outline-danger">Delete</button>
   </form>
   ```

> ✓ **Quick check:** `dotnet build` succeeds with no warnings.

### **Step 10:** Run locally and exercise the full applicant-then-recruiter flow

A real end-to-end pass before you reach for Docker. The point is to see all the pieces — the validator, the upload, the in-memory store, the recruiter views — connected and working in a single process.

1. **Start** the app:

   ```bash
   dotnet run --launch-profile http
   ```

   Note the port; call it `<port>` below.

2. **Open** `http://localhost:<port>/` in a browser. You should see four job cards.

3. **Click** *Apply* on any job, fill in name and email, attach a real PDF (any small one), and submit. You should land on `/Applications/Details/<some-guid>` with a green "Thanks for applying!" banner.

4. **Try** uploading a non-PDF: rename a `notes.txt` file to `cv.pdf` and submit. The form should re-render with a validation error — *"The uploaded file is not a valid PDF document."*

5. **Visit** `http://localhost:<port>/Applications`. The successful application appears in the table.

6. **Open** the detail page, change the status to `UnderReview`, save, and confirm the badge updates back on the listing.

7. **Click** the *View CV* link — the PDF should render inline in a new tab.

8. **Stop** the server with `Ctrl+C`. The `uploads/` directory now contains your test CV — confirm the file size and that opening it in a real PDF viewer works. Delete the directory if you want a clean slate.

> ✓ **Quick check:** Four jobs on the home page, valid PDF accepted, invalid file rejected with a clear error, status updates persist within the running process, CV renders inline.

### **Step 11:** Containerize, set up the OIDC-federated pipeline, and deploy

Same OIDC pattern as the previous chapter — fresh Azure resources, a fresh Entra app registration with a federated credential, a workflow that signs in via OIDC, builds, pushes, updates the Container App, and smoke-tests the deployment. Container ingress on `8080`. The smoke target for this exercise is `/` (the anonymous home page) — the deeper `/health/live` endpoint that future-you will smoke against doesn't exist yet, and trying to use it here would fail every deploy until the third exercise lands. See the deployment chapter for why `id-token: write` is load-bearing and how the federated subject claim is matched.

1. **Add** the Dockerfile at the project root:

   > `Dockerfile`

   ```dockerfile
   # Build stage — full SDK, restores and publishes a Release build.
   FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
   WORKDIR /src
   COPY *.csproj ./
   RUN dotnet restore
   COPY . ./
   RUN dotnet publish -c Release -o /app /p:UseAppHost=false

   # Runtime stage — slim ASP.NET image, runs as the non-root 'app' user.
   FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
   WORKDIR /app
   COPY --from=build /app ./

   # LocalFileBlobService writes uploads to ContentRoot/uploads at runtime.
   # /app is owned by root, so create the directory and chown it to the non-root
   # 'app' user before dropping privileges. (Replaced by Azure Blob in the next
   # exercise; harmless to keep either way.)
   RUN mkdir -p /app/uploads && chown app:app /app/uploads

   EXPOSE 8080
   ENV ASPNETCORE_URLS=http://+:8080
   USER app

   ENTRYPOINT ["dotnet", "CloudCiCareers.Web.dll"]
   ```

2. **Add** a `.dockerignore`:

   > `.dockerignore`

   ```text
   bin/
   obj/
   .vs/
   .vscode/
   .idea/
   *.user
   .git/
   .github/
   uploads/
   Dockerfile
   .dockerignore
   README.md
   ```

3. **Push** the project to a fresh GitHub repo (substitute your username for `<your-gh-user>`; the example uses `larsappel`):

   ```bash
   git init
   git add .
   git commit -m "Scaffold CloudCiCareers.Web with PDF uploads"
   gh repo create <your-gh-user>/cloudci-careers --public --source=. --push
   ```

4. **Provision** the Azure resources. Pick a 5–6 character random alphanumeric suffix for the ACR name (it must be globally unique); the example uses `7g8h2j` but you should generate your own:

   ```bash
   az group create -n rg-careers-week7 -l northeurope

   az acr create -n acrcareers<rand> -g rg-careers-week7 \
     --sku Basic --admin-enabled false

   az containerapp env create -n cae-careers-week7 \
     -g rg-careers-week7 -l northeurope

   az containerapp create \
     -n ca-careers-week7 \
     -g rg-careers-week7 \
     --environment cae-careers-week7 \
     --image mcr.microsoft.com/k8se/quickstart:latest \
     --target-port 8080 \
     --ingress external \
     --min-replicas 1 \
     --max-replicas 1
   ```

5. **Create** the Entra app, the service principal, the role assignments, and the federated credential. Substitute your GitHub username for `<your-gh-user>` in the federated subject — leaving the angle brackets in literally is the single most common authentication failure:

   ```bash
   az ad app create --display-name "github-cloudci-careers-oidc"
   export APP_ID="<paste appId from previous output>"
   az ad sp create --id "$APP_ID"

   ACR_ID=$(az acr show -n acrcareers<rand> --query id -o tsv)
   CA_ID=$(az containerapp show -n ca-careers-week7 \
     -g rg-careers-week7 --query id -o tsv)

   az role assignment create --assignee "$APP_ID" \
     --role AcrPush --scope "$ACR_ID"
   az role assignment create --assignee "$APP_ID" \
     --role "Container Apps Contributor" --scope "$CA_ID"

   az ad app federated-credential create \
     --id "$APP_ID" \
     --parameters '{
       "name": "main-branch",
       "issuer": "https://token.actions.githubusercontent.com",
       "subject": "repo:<your-gh-user>/cloudci-careers:ref:refs/heads/main",
       "audiences": ["api://AzureADTokenExchange"],
       "description": "GitHub Actions, main branch only"
     }'
   ```

6. **Set** the four GitHub secrets via stdin (avoids leaving values in shell history):

   ```bash
   printf '%s' "$APP_ID" | gh secret set AZURE_CLIENT_ID \
     --repo <your-gh-user>/cloudci-careers
   printf '%s' "$(az account show --query tenantId -o tsv)" | \
     gh secret set AZURE_TENANT_ID --repo <your-gh-user>/cloudci-careers
   printf '%s' "$(az account show --query id -o tsv)" | \
     gh secret set AZURE_SUBSCRIPTION_ID --repo <your-gh-user>/cloudci-careers
   printf '%s' "acrcareers<rand>" | gh secret set ACR_NAME \
     --repo <your-gh-user>/cloudci-careers
   ```

7. **Grant** the Container App's managed identity `AcrPull`, then point the app at the registry over that identity:

   ```bash
   az containerapp identity assign \
     -n ca-careers-week7 -g rg-careers-week7 --system-assigned

   IDENTITY_PRINCIPAL_ID=$(az containerapp show \
     -n ca-careers-week7 -g rg-careers-week7 \
     --query identity.principalId -o tsv)

   az role assignment create \
     --assignee "$IDENTITY_PRINCIPAL_ID" \
     --role AcrPull --scope "$ACR_ID"

   az containerapp registry set \
     -n ca-careers-week7 -g rg-careers-week7 \
     --server acrcareers<rand>.azurecr.io \
     --identity system
   ```

8. **Add** the workflow at `.github/workflows/ci.yml`:

   > `.github/workflows/ci.yml`

   ```yaml
   name: build-and-deploy

   on:
     push:
       branches: [main]
     workflow_dispatch:

   permissions:
     id-token: write
     contents: read

   env:
     RESOURCE_GROUP: rg-careers-week7
     CONTAINER_APP: ca-careers-week7

   jobs:
     deploy:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4

         - name: Sign in to Azure
           uses: azure/login@v2
           with:
             client-id: ${{ secrets.AZURE_CLIENT_ID }}
             tenant-id: ${{ secrets.AZURE_TENANT_ID }}
             subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

         - name: Resolve ACR login server
           run: |
             echo "ACR_LOGIN_SERVER=${{ secrets.ACR_NAME }}.azurecr.io" \
               >> "$GITHUB_ENV"

         - name: Log in to ACR
           run: az acr login --name ${{ secrets.ACR_NAME }}

         - name: Build and push image
           uses: docker/build-push-action@v6
           with:
             context: .
             push: true
             tags: |
               ${{ env.ACR_LOGIN_SERVER }}/cloudci-careers:${{ github.sha }}
               ${{ env.ACR_LOGIN_SERVER }}/cloudci-careers:latest

         - name: Update Container App
           run: |
             az containerapp update \
               -n "$CONTAINER_APP" -g "$RESOURCE_GROUP" \
               --image "${{ env.ACR_LOGIN_SERVER }}/cloudci-careers:${{ github.sha }}"

         - name: Smoke test
           run: |
             FQDN=$(az containerapp show \
               -n "$CONTAINER_APP" -g "$RESOURCE_GROUP" \
               --query properties.configuration.ingress.fqdn -o tsv)
             for i in {1..20}; do
               if curl -fsS "https://$FQDN/" >/dev/null; then
                 echo "Smoke test passed."
                 exit 0
               fi
               sleep 3
             done
             echo "Smoke test failed."
             exit 1
   ```

9. **Push** to trigger the first real deployment:

   ```bash
   git add Dockerfile .dockerignore .github/workflows/ci.yml
   git commit -m "Add OIDC-federated build-and-deploy workflow"
   git push
   gh run watch
   ```

10. **Verify** the deployed FQDN serves the home page:

    ```bash
    FQDN=$(az containerapp show \
      -n ca-careers-week7 -g rg-careers-week7 \
      --query properties.configuration.ingress.fqdn -o tsv)
    echo "https://$FQDN/"
    curl -I "https://$FQDN/"
    ```

    Expected: `HTTP/2 200`. Open the URL — the four job cards render.

> ⚠ **Common Mistakes**
>
> - Pointing the smoke test at `/health/live`. That endpoint doesn't exist yet — it lands in the third exercise of this chapter. Pointing the smoke step at it now causes every deploy to fail.
> - Federated subject typo: `refs/head/main` instead of `refs/heads/main`. The token is rejected with `AADSTS70021: No matching federated identity record found`.
> - `--target-port` not `8080`. Container Apps' health probe fails because Kestrel listens on `:8080` (set by the Dockerfile `ENV`) but the platform asks port-something-else.
>
> ✓ **Quick check:** The workflow run is green, `https://$FQDN/` returns `200`, and the home page lists four jobs.

### **Step 12:** Add Application Insights via the `secretref:` pattern

Same pattern as the logging chapter — workspace-based component, SDK pinned to 2.22.0, connection string injected as a Container Apps secret and referenced by env var. The pin is **mandatory** here too: the 3.x line throws `System.InvalidOperationException: A connection string was not found...` at host startup when the env var is empty (i.e. every `dotnet run` on your laptop), while 2.22.0 silently drops telemetry when missing.

1. **Provision** the workspace-based component against the auto-managed Log Analytics workspace inside `rg-careers-week7`:

   ```bash
   WS_ID=$(az monitor log-analytics workspace list \
     -g rg-careers-week7 --query '[0].id' -o tsv)

   az monitor app-insights component create \
     --app cloudci-careers-insights \
     -g rg-careers-week7 --location northeurope \
     --workspace "$WS_ID"

   CONN=$(az monitor app-insights component show \
     --app cloudci-careers-insights -g rg-careers-week7 \
     --query connectionString -o tsv)
   ```

2. **Add** the SDK and register it:

   ```bash
   dotnet add package Microsoft.ApplicationInsights.AspNetCore --version 2.22.0
   ```

   In `Program.cs`, alongside the other service registrations:

   > `Program.cs`

   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

3. **Inject** the connection string and bind the env var:

   ```bash
   az containerapp secret set \
     -g rg-careers-week7 -n ca-careers-week7 \
     --secrets appinsights-connstr="$CONN"

   az containerapp update \
     -g rg-careers-week7 -n ca-careers-week7 \
     --set-env-vars APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-connstr
   ```

4. **Commit and push** to roll the SDK out:

   ```bash
   git add CloudCiCareers.Web.csproj Program.cs
   git commit -m "Add Application Insights SDK"
   git push
   gh run watch
   ```

> ✓ **Quick check:** `az containerapp show -g rg-careers-week7 -n ca-careers-week7 --query 'properties.template.containers[0].env'` shows `APPLICATIONINSIGHTS_CONNECTION_STRING` with a `secretRef`, not a literal value.

### **Step 13:** Test Your Implementation

End-to-end check against the live deployment.

1. **Capture** the FQDN:

   ```bash
   FQDN=$(az containerapp show \
     -n ca-careers-week7 -g rg-careers-week7 \
     --query properties.configuration.ingress.fqdn -o tsv)
   echo "https://$FQDN/"
   ```

2. **Verify** the home page lists 3–4 jobs:

   ```bash
   curl -I "https://$FQDN/"
   ```

   Expected: `HTTP/2 200`.

3. **Apply** for a job in the browser with a valid PDF. Confirm:

   - The form posts and you land on `/Applications/Details/<guid>`.
   - The success banner shows.
   - The application appears in `https://$FQDN/Applications`.

4. **Re-submit** the form with a non-PDF (rename `notes.txt` to `cv.pdf`). The form re-renders with the validation error, no application is created.

5. **Edit** the status on the detail page → the badge updates on the listing.

6. **Click** *View CV* → the PDF renders inline in the browser tab.

7. **Generate** traffic and check Application Insights:

   ```bash
   for i in {1..30}; do curl -s "https://$FQDN/" >/dev/null; sleep 0.3; done
   ```

   In the Azure Portal, navigate to Application Insights `cloudci-careers-insights` → **Logs**, and run:

   ```kusto
   requests
   | where timestamp > ago(10m)
   | summarize count() by name, resultCode
   ```

   You should see incoming requests within one to three minutes of the curl loop.

> ✓ **Success indicators:**
>
> - `https://$FQDN/` returns `200` and lists four jobs
> - Valid PDF upload creates an application visible at `/Applications`
> - Invalid file (renamed `notes.txt`) is rejected with a clear validation message
> - Status edits persist between page loads
> - The CV link renders the original PDF inline
> - App Insights `requests` table records traffic against the deployed FQDN
>
> ✓ **Final verification checklist:**
>
> - ☐ `CloudCiCareers.Web` project scaffolded with `dotnet new mvc --framework net10.0`
> - ☐ `Job`, `Application` + `ApplicationStatus`, and `ApplyForm` types under `Models/`
> - ☐ `IJobCatalog` + `StaticJobCatalog`, `IApplicationStore` + `InMemoryApplicationStore`, `IBlobService` + `LocalFileBlobService`, plus `PdfValidation` under `Services/`
> - ☐ `JobsController` + `Apply.cshtml` + `Index.cshtml`, `ApplicationsController` + `Index.cshtml` + `Details.cshtml`
> - ☐ Dockerfile produces a working image listening on `:8080` running as non-root
> - ☐ GitHub repo `<your-gh-user>/cloudci-careers` pushed and federated against an Entra app `github-cloudci-careers-oidc`
> - ☐ Workflow runs green on push to `main`, deploys to Container App `ca-careers-week7` in `rg-careers-week7`, smoke-tests `/`
> - ☐ App Insights `cloudci-careers-insights` workspace-based, connection string delivered via `secretref:`

Persistence in this exercise is fragile by design — the in-memory store and the `./uploads/` directory are wiped on every Container App revision rollover, every restart, every scale event. Apply for a job, redeploy, and watch the application disappear. This is **not** a bug to fix here; it's the motivating problem for what comes next.

## Common Issues

> **If you encounter problems:**
>
> **`The required antiforgery cookie ... is not present` or `Antiforgery token mismatch`:** Either `@Html.AntiForgeryToken()` is missing from the form or `[ValidateAntiForgeryToken]` is missing from the action. MVC's default scaffolding adds neither — you have to.
>
> **`System.InvalidOperationException: A connection string was not found for the Application Insights connection.`:** The Application Insights SDK package on disk is 3.x. Check `dotnet list package` — confirm `Microsoft.ApplicationInsights.AspNetCore 2.22.0`. If it's drifted, `dotnet remove package` then re-add with `--version 2.22.0`.
>
> **Smoke test fails on every deploy with `curl: (22) The requested URL returned error: 404`:** The workflow is hitting `/health/live` or some other endpoint that doesn't exist yet. The smoke target during this exercise must be `/`. The deeper health probes arrive later in this chapter.
>
> **`AADSTS70021: No matching federated identity record found for presented assertion`:** The `subject` claim from the GitHub OIDC token doesn't match the federated credential. Exact form: `repo:<your-gh-user>/cloudci-careers:ref:refs/heads/main`. Watch for `head` vs `heads`, the wrong username, the wrong branch, or angle brackets left in literally.
>
> **Workflow fails at the push step with `unauthorized: authentication required` or 401:** The federated subject is correct but the Entra app doesn't have `AcrPush` on the registry yet, or role-assignment propagation hasn't finished. Wait one to two minutes and re-run.
>
> **Apply form rejects valid PDFs:** `PdfValidation.IsPdf` is reading the four bytes but not rewinding. Confirm the `s.Position = 0` line. Symptom: the saved file in `uploads/` is exactly four bytes shorter than the original and starts with whatever followed `%PDF`.
>
> **Container App revision goes `Failed` with `Failed to pull image`:** Either the system-assigned managed identity wasn't granted `AcrPull`, or `az containerapp registry set --identity system` was skipped. Both are required.
>
> **`Live Metrics: Not available`:** The connection string env var didn't make it into the container. Check `az containerapp show ... --query 'properties.template.containers[0].env'` for the `secretRef` and `az containerapp secret list ...` for the underlying value.
>
> **Still stuck?** Re-read the deployment chapter for the OIDC pipeline mechanics and the logging chapter for the `secretref:` pattern. The names changed; the patterns didn't.

## Summary

You have a working anonymous recruitment portal — applicants browse four hard-coded jobs, attach a PDF, and submit, while recruiters see every application and walk it through a status workflow. The PDF validation actually checks the file's bytes rather than trusting the extension. The whole thing builds into a non-root multi-stage Docker image, ships through an OIDC-federated GitHub Actions pipeline to a Container App in `rg-careers-week7`, and emits telemetry to Application Insights via the `secretref:` pattern.

- ✓ MVC project shape — controllers + Razor views + antiforgery on every form — fits a portal with shared layout and multiple actions per resource
- ✓ Form-binding types (`ApplyForm`) are wire types; entities (`Application`) are domain types — same discipline as DTOs in the previous chapter
- ✓ The four-byte `%PDF` magic-bytes check is the load-bearing piece of file-type validation; magic bytes confirm the file *is* a PDF, not that the PDF is *safe*
- ✓ `IFormFile.OpenReadStream()` pipes straight to `IBlobService.UploadAsync` — don't re-buffer through `MemoryStream`
- ✓ The `IBlobService` abstraction is what makes the next exercise's swap to Azure Blob a one-line registration change
- ✓ The OIDC-federated pipeline pattern from the deployment chapter ports cleanly to a brand-new repo, with a smoke target (`/`) that exists today

> **Key takeaway:** Storage in this exercise is intentionally throwaway — the in-memory dictionary and the local `uploads/` folder are wiped on every revision rollover. The motivation for managed state is much sharper after you've felt the fragility of process-local state in production than after reading about it. Stop here, deploy a few applications, redeploy, watch them vanish — that lived experience is what makes the next change feel like a fix instead of a chore.

## Going Deeper (Optional)

> **Want to explore more?**
>
> - Antivirus scanning at rest with **Microsoft Defender for Storage** — once CVs land in Azure Blob, Defender can scan each new blob and quarantine matches.
> - Larger-than-30 MB uploads via **direct-to-blob with SAS** — the browser PUTs straight to a pre-signed blob URL, skipping the application server entirely. The default end-to-end limit (Container Apps ingress + Kestrel + ASP.NET Core multipart, all 30 MB) only matters for the through-the-server path.
> - Content stripping with **PdfPig** — parse the uploaded PDF and re-emit only text and known-safe constructs, dropping JavaScript and embedded files. Layer two on top of magic bytes.
> - The **OWASP File Upload Cheat Sheet** for the broader threat model: <https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html>.

## Done!

The portal runs in the cloud. Anyone with the FQDN can browse jobs, submit applications, and walk them through the recruiter workflow. It works — until the next deploy, when the in-memory store and the uploaded PDFs both vanish. In the next exercise we replace the in-memory store with managed state in CosmosDB and the local files with Azure Blob, both authenticated via the Container App's managed identity. The controller code does not change; only the registrations in `Program.cs` do.
