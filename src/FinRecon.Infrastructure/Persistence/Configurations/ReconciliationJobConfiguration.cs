using FinRecon.Core.Domain.Entities;
using FinRecon.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinRecon.Infrastructure.Persistence.Configurations;

public class ReconciliationJobConfiguration : IEntityTypeConfiguration<ReconciliationJob>
{
    public void Configure(EntityTypeBuilder<ReconciliationJob> builder)
    {
        builder.ToTable("reconciliation_jobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Filename)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.FileHash)
            .HasMaxLength(64) // SHA-256 hex
            .IsRequired();

        builder.Property(x => x.StorageKey)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ReferenceDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // DB-level duplicate detection: same file for same date is rejected
        builder.HasIndex(x => new { x.FileHash, x.ReferenceDate })
            .IsUnique()
            .HasDatabaseName("ix_reconciliation_jobs_hash_date");
    }
}
