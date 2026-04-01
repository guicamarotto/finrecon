using FinRecon.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinRecon.Infrastructure.Persistence.Configurations;

public class ReconciliationReportConfiguration : IEntityTypeConfiguration<ReconciliationReport>
{
    public void Configure(EntityTypeBuilder<ReconciliationReport> builder)
    {
        builder.ToTable("reconciliation_reports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalDelta)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(x => x.GeneratedAt)
            .IsRequired();

        builder.HasIndex(x => x.JobId)
            .IsUnique()
            .HasDatabaseName("ix_reconciliation_reports_job_id");
    }
}
