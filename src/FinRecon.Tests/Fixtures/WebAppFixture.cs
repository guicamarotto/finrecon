using FinRecon.Core.Interfaces;
using FinRecon.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace FinRecon.Tests.Fixtures;

/// <summary>
/// WebApplicationFactory that replaces the real database with a Testcontainers Postgres instance
/// and replaces MassTransit with the InMemory transport so tests don't need a real RabbitMQ broker.
/// </summary>
public class WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("finrecon_api_test")
        .WithUsername("finrecon_test")
        .WithPassword("test_password")
        .Build();

    public async Task InitializeAsync()
    {
        // Set env vars before the server starts — WebApplication.CreateBuilder() reads these
        // before Program.cs inline code runs, so ConfigureWebHost.ConfigureAppConfiguration is too late.
        Environment.SetEnvironmentVariable("Jwt__Secret", "finrecon-test-secret-key-32-chars!!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "finrecon-test");
        Environment.SetEnvironmentVariable("Jwt__Audience", "finrecon-test");
        await _postgres.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _postgres.GetConnectionString());

        // Trigger server creation and apply EF migrations to the test database
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinReconDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "finrecon-test-secret-key-32-chars!!",
                ["Jwt:Issuer"] = "finrecon-test",
                ["Jwt:Audience"] = "finrecon-test",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace real DbContext with test Postgres
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FinReconDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<FinReconDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Replace MassTransit with InMemory transport — no real RabbitMQ needed
            var massTransitDescriptors = services.Where(d =>
                d.ServiceType.FullName?.Contains("MassTransit") == true).ToList();
            foreach (var d in massTransitDescriptors) services.Remove(d);

            services.AddMassTransitTestHarness();

            // Replace MinIO with a no-op stub — MinioFileStorageService throws on construction
            // when Endpoint is null (ServiceURL = "http://"), causing UriFormatException in AmazonS3Client
            var storageDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IFileStorageService));
            if (storageDescriptor != null) services.Remove(storageDescriptor);
            services.AddScoped<IFileStorageService, NullFileStorageService>();
        });
    }

    private sealed class NullFileStorageService : IFileStorageService
    {
        public Task<string> UploadAsync(string filename, Stream content, string contentType, CancellationToken ct = default)
            => Task.FromResult($"test/{Guid.NewGuid()}/{filename}");

        public Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
            => Task.FromResult<Stream>(Stream.Null);
    }

    public new async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", null);
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
