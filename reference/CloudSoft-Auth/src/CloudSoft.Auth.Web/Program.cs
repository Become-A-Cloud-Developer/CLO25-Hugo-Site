using CloudSoft.Auth.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Data: store provider chosen by the IdentityStore:Provider flag.
// InMemory — transient, reseeded each boot. SQLite — file-based, persistent.
var storeProvider = builder.Configuration["IdentityStore:Provider"] ?? "InMemory";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    switch (storeProvider.ToLowerInvariant())
    {
        case "sqlite":
            var connectionString = builder.Configuration.GetConnectionString("Identity")
                ?? "Data Source=cloudsoft-auth.db";
            options.UseSqlite(connectionString);
            break;

        case "inmemory":
        default:
            options.UseInMemoryDatabase("CloudSoftAuthDb");
            break;
    }
});

// Identity: user + role management, scoped to ApplicationUser.
// Password requirements are relaxed to keep the hardcoded Chapter-4 passwords
// (admin/admin, candidate/candidate) working. Production apps should leave
// the defaults in place.
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 3;
        options.User.RequireUniqueEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Google OAuth is conditionally registered.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication().AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireEngineering", policy =>
        policy.RequireClaim("Department", "Engineering"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// For SQLite, create the schema if this is the first run. InMemory doesn't need it.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.EnsureCreated();
    }
}

// Seed roles and (from configuration) the admin user. In Development the
// candidate lab user is also created automatically.
await IdentitySeeder.SeedAsync(app.Services, app.Environment);

app.Run();
