using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinRecon.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePendingSeedJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "reconciliation_jobs",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "reconciliation_jobs",
                columns: new[] { "Id", "CompletedAt", "CreatedAt", "FileHash", "Filename", "ReferenceDate", "Status", "StorageKey" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, new DateTime(2025, 1, 15, 9, 0, 0, 0, DateTimeKind.Utc), "cafebabecafebabecafebabecafebabe01010101010101010101010101010101", "portfolio_2025-01-15.csv", new DateOnly(2025, 1, 15), "Pending", "2025-01-15/22222222-2222-2222-2222-222222222222/portfolio_2025-01-15.csv" });
        }
    }
}
