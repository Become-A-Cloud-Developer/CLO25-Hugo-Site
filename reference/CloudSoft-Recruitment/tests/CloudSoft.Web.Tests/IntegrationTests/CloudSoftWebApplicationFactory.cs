using CloudSoft.Web.Data;
using CloudSoft.Web.Repositories;
using CloudSoft.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudSoft.Web.Tests.IntegrationTests;

public class CloudSoftWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKeyForIntegrationTestsThatIsAtLeast32Characters!",
                ["Jwt:Issuer"] = "CloudSoft.Tests",
                ["Jwt:Audience"] = "CloudSoft.Tests",
                ["Jwt:ExpirationMinutes"] = "60",
                ["AdminSeed:Email"] = "admin@cloudsoft.com",
                ["AdminSeed:Password"] = "Admin123!",
                ["CandidateSeed:Email"] = "candidate@test.com",
                ["CandidateSeed:Password"] = "Candidate123!"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace ApplicationDbContext with a unique SQLite in-memory database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            var dbName = $"IntegrationTest_{Guid.NewGuid()}.db";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={dbName}"));

            // Ensure in-memory repositories (override any MongoDB config)
            ReplaceService<IJobRepository, InMemoryJobRepository>(services, ServiceLifetime.Singleton);
            ReplaceService<IApplicationRepository, InMemoryApplicationRepository>(services, ServiceLifetime.Singleton);

            // Use local blob service
            var blobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBlobService));
            if (blobDescriptor != null)
                services.Remove(blobDescriptor);

            var uploadPath = Path.Combine(Path.GetTempPath(), $"cloudsoft-tests-{Guid.NewGuid()}");
            services.AddSingleton<IBlobService>(new LocalBlobService(uploadPath));
        });
    }

    private static void ReplaceService<TService, TImplementation>(
        IServiceCollection services, ServiceLifetime lifetime)
        where TService : class
        where TImplementation : class, TService
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor != null)
            services.Remove(descriptor);

        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
    }
}
