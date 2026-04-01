using FinRecon.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinRecon.Infrastructure.Persistence.Configurations;

public class ReconciliationRecordConfiguration : IEntityTypeConfiguration<ReconciliationRecord>
{
    public void Configure(EntityTypeBuilder<ReconciliationRecord> builder)
    {
        builder.ToTable("reconciliation_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ProductType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CurrentValue)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(x => x.PreviousValue)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.Delta)
            .HasColumnType("decimal(18,4)");

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.JobId)
            .HasDatabaseName("ix_reconciliation_records_job_id");

        builder.HasIndex(x => new { x.JobId, x.Status })
            .HasDatabaseName("ix_reconciliation_records_job_status");
    }
}
