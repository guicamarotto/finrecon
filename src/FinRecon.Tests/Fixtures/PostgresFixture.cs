using FinRecon.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FinRecon.Tests.Fixtures;

/// <summary>
/// Starts a real PostgreSQL 16 container for integration tests.
/// Uses Testcontainers to avoid SQLite-specific behaviours that would mask
/// Postgres-specific failures (enum storage, constraint enforcement, etc.).
/// </summary>
[CollectionDefinition("postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture> { }

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("finrecon_test")
        .WithUsername("finrecon_test")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<FinReconDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new FinReconDbContext(options);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public FinReconDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FinReconDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new FinReconDbContext(options);
    }
}
