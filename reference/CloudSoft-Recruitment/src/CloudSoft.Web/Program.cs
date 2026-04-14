using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using CloudSoft.Web.Data;
using CloudSoft.Web.Middleware;
using CloudSoft.Web.Models;
using CloudSoft.Web.Options;
using CloudSoft.Web.Repositories;
using CloudSoft.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Azure Key Vault configuration — load FIRST so secrets override env vars
// before any service registrations read connection strings.
bool useKeyVault = builder.Configuration.GetValue<bool>("FeatureFlags:UseKeyVault");
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (useKeyVault && !string.IsNullOrEmpty(keyVaultUri))
{
    var managedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new Azure.Identity.DefaultAzureCredential(new Azure.Identity.DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = managedIdentityClientId
        }));
    Console.WriteLine("Azure Key Vault enabled");
}
else
{
    Console.WriteLine("Azure Key Vault disabled — using local configuration");
}

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Configure Identity database (SQLite — only for Identity, not for domain data)
var connectionString = builder.Configuration.GetConnectionString("Identity")
    ?? "Data Source=identity.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure external authentication (Google OAuth + JWT)
bool useGoogleAuth = builder.Configuration.GetValue<bool>("FeatureFlags:UseGoogleAuth");
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];

var authBuilder = builder.Services.AddAuthentication();

if (useGoogleAuth && !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
    Console.WriteLine("Google OAuth enabled");
}
else
{
    Console.WriteLine("Google OAuth disabled");
}

authBuilder.AddJwtBearer();

builder.Services.AddOptions<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(
                    configuration["Jwt:Key"] ?? "placeholder-key-not-for-production-use-replace-me!!"))
        };
    });

// Configure authentication cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Data Protection key persistence
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Environment.IsDevelopment()
            ? Path.Combine(Directory.GetCurrentDirectory(), "data", "keys")
            : "/app/data/keys"));

// Configure MongoDB options
builder.Services.Configure<MongoDbOptions>(
    builder.Configuration.GetSection(MongoDbOptions.SectionName));

// Configure Blob Storage (feature flag + connection string required)
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName));
bool useBlobStorage = builder.Configuration.GetValue<bool>("FeatureFlags:UseBlobStorage");
var blobConnectionString = builder.Configuration[$"{BlobStorageOptions.SectionName}:ConnectionString"];
if (useBlobStorage && !string.IsNullOrEmpty(blobConnectionString))
{
    builder.Services.AddSingleton<IBlobService, BlobService>();
    Console.WriteLine("Using Azure Blob Storage for uploads");
}
else
{
    var localPath = builder.Environment.IsDevelopment()
        ? Path.Combine(Directory.GetCurrentDirectory(), "data", "uploads")
        : "/app/data/uploads";
    builder.Services.AddSingleton<IBlobService>(new LocalBlobService(localPath));
    Console.WriteLine($"Using local file storage for uploads: {localPath}");
}

// Feature flag: use MongoDB or in-memory repository
bool useMongoDB = builder.Configuration.GetValue<bool>("FeatureFlags:UseMongoDB");

if (useMongoDB)
{
    builder.Services.AddSingleton<IJobRepository, MongoDbJobRepository>();
    builder.Services.AddSingleton<IApplicationRepository, MongoDbApplicationRepository>();
    Console.WriteLine("Using MongoDB repository");
}
else
{
    builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();
    builder.Services.AddSingleton<IApplicationRepository, InMemoryApplicationRepository>();
    Console.WriteLine("Using in-memory repository");
}

// Register services
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();

// Configure JWT options and token service
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();

// REST Countries service (feature flag)
bool useRestCountries = builder.Configuration.GetValue<bool>("FeatureFlags:UseRestCountries");
if (useRestCountries)
{
    builder.Services.AddHttpClient<ICountryService, CountryService>(client =>
    {
        client.BaseAddress = new Uri("https://restcountries.com/");
        client.Timeout = TimeSpan.FromSeconds(5);
    });
    Console.WriteLine("REST Countries API enabled");
}
else
{
    builder.Services.AddSingleton<ICountryService, DisabledCountryService>();
    Console.WriteLine("REST Countries API disabled");
}

builder.Services.AddHealthChecks();

// Application Insights telemetry (feature flag + connection string required)
bool useApplicationInsights = builder.Configuration.GetValue<bool>("FeatureFlags:UseApplicationInsights");
var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (useApplicationInsights && !string.IsNullOrEmpty(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
    Console.WriteLine("Application Insights enabled");
}
else
{
    Console.WriteLine("Application Insights disabled — logging to console only");
}

var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    context.Database.Migrate();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = ["Admin", "Candidate"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var adminEmail = builder.Configuration["AdminSeed:Email"] ?? "admin@cloudsoft.com";
    var adminPassword = builder.Configuration["AdminSeed:Password"];

    if (!string.IsNullOrEmpty(adminPassword))
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "Admin"
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine($"Admin user '{adminEmail}' seeded successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to seed admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }

    // Seed candidate user when password is configured (any environment)
    {
        var candidateEmail = builder.Configuration["CandidateSeed:Email"] ?? "candidate@test.com";
        var candidatePassword = builder.Configuration["CandidateSeed:Password"];

        if (!string.IsNullOrEmpty(candidatePassword))
        {
            var candidateUser = await userManager.FindByEmailAsync(candidateEmail);
            if (candidateUser == null)
            {
                candidateUser = new ApplicationUser
                {
                    UserName = candidateEmail,
                    Email = candidateEmail,
                    FirstName = "Test",
                    LastName = "Candidate"
                };

                var result = await userManager.CreateAsync(candidateUser, candidatePassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(candidateUser, "Candidate");
                    Console.WriteLine($"Candidate '{candidateEmail}' seeded successfully.");
                }
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Trust forwarded headers from ACA reverse proxy (required for correct
    // HTTPS redirect URIs in Google OAuth behind TLS-terminating ingress)
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString()
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapGet("/version", () => Results.Ok(new
{
    version = typeof(Program).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "unknown"
}));

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
