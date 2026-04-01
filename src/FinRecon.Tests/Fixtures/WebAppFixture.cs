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
        await _postgres.StartAsync();
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
        });
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
