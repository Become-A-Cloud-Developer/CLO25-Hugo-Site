var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register Application Insights. Reads the connection string from the
// APPLICATIONINSIGHTS_CONNECTION_STRING environment variable by default.
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Generates a per-request correlation ID and pushes it onto the
// logger scope so every log call inside the request carries it.
app.Use(async (context, next) =>
{
    var correlationId = Guid.NewGuid().ToString("N");
    context.Items["CorrelationId"] = correlationId;

    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("CorrelationIdMiddleware");

    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["RequestId"] = correlationId
    }))
    {
        await next();
    }
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
