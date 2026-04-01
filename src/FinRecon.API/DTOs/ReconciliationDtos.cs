using FinRecon.Core.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace FinRecon.API.DTOs;

public record UploadReconciliationRequest(IFormFile File, DateOnly ReferenceDate);

public record CreateReconciliationResponse(Guid JobId, string Status);

public record ReconciliationJobDto(
    Guid Id,
    string Filename,
    string Status,
    DateOnly ReferenceDate,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record ReconciliationRecordDto(
    Guid Id,
    string ClientId,
    string ProductType,
    decimal CurrentValue,
    decimal? PreviousValue,
    decimal? Delta,
    string Status
);

public record ReconciliationReportDto(
    Guid Id,
    Guid JobId,
    int TotalRecords,
    int Matched,
    int Discrepant,
    int NewRecords,
    int MissingRecords,
    decimal TotalDelta,
    DateTime GeneratedAt
);

public record ReconciliationDetailDto(
    ReconciliationJobDto Job,
    ReconciliationReportDto? Report
);

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize
);

public record ErrorResponse(string Code, string Message);
