using FinRecon.Core.Domain.Entities;
using FinRecon.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinRecon.Infrastructure.Persistence;

public class FinReconDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<ReconciliationJob> ReconciliationJobs => Set<ReconciliationJob>();
    public DbSet<ReconciliationRecord> ReconciliationRecords => Set<ReconciliationRecord>();
    public DbSet<ReconciliationReport> ReconciliationReports => Set<ReconciliationReport>();

    public FinReconDbContext(DbContextOptions<FinReconDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ReconciliationJobConfiguration());
        modelBuilder.ApplyConfiguration(new ReconciliationRecordConfiguration());
        modelBuilder.ApplyConfiguration(new ReconciliationReportConfiguration());

        SeedData.Apply(modelBuilder);
    }
}

public class ApplicationUser : IdentityUser { }
