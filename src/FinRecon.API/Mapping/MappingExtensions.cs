using FinRecon.API.DTOs;
using FinRecon.Core.Common;
using FinRecon.Core.Domain.Entities;

namespace FinRecon.API.Mapping;

public static class MappingExtensions
{
    public static ReconciliationJobDto ToDto(this ReconciliationJob job)
        => new(job.Id, job.Filename, job.Status.ToString(), job.ReferenceDate, job.CreatedAt, job.CompletedAt);

    public static ReconciliationRecordDto ToDto(this ReconciliationRecord record)
        => new(record.Id, record.ClientId, record.ProductType.ToString(),
               record.CurrentValue, record.PreviousValue, record.Delta, record.Status.ToString());

    public static ReconciliationReportDto ToDto(this ReconciliationReport report)
        => new(report.Id, report.JobId, report.TotalRecords, report.Matched, report.Discrepant,
               report.NewRecords, report.MissingRecords, report.TotalDelta, report.GeneratedAt);

    public static PagedResultDto<T> ToDto<TSource, T>(this PagedResult<TSource> source, Func<TSource, T> map)
        => new(source.Items.Select(map).ToList(), source.Total, source.Page, source.PageSize);
}
