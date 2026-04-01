using FinRecon.Core.Interfaces;
using FinRecon.Core.Services;
using FinRecon.Infrastructure.Persistence;
using FinRecon.Infrastructure.Persistence.Repositories;
using FinRecon.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinRecon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<FinReconDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(FinReconDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IReconciliationRepository, ReconciliationRepository>();

        // Storage
        services.Configure<MinioOptions>(configuration.GetSection("MinIO"));
        services.AddScoped<IFileStorageService, MinioFileStorageService>();

        // Domain services
        services.AddTransient<IReconciliationEngine, ReconciliationEngine>();

        return services;
    }
}
