using System.Text.Json;
using System.Text.Json.Serialization;
using FinRecon.Core.Common;
using FinRecon.Core.Domain.Enums;
using FinRecon.Core.Domain.Errors;
using FinRecon.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FinRecon.Worker.Parsers;

public class JsonFileParser : IFileParser
{
    private readonly ILogger<JsonFileParser> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) }
    };

    public JsonFileParser(ILogger<JsonFileParser> logger) => _logger = logger;

    public bool CanParse(string filename)
        => filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    public async Task<Result<IReadOnlyList<FileRecord>>> ParseAsync(Stream content, CancellationToken ct = default)
    {
        try
        {
            var payload = await JsonSerializer.DeserializeAsync<JsonPayload>(content, JsonOptions, ct);

            if (payload?.Records is null)
                return Result<IReadOnlyList<FileRecord>>.Fail(DomainErrors.File.InvalidSchema);

            var records = new List<FileRecord>(payload.Records.Count);
            foreach (var item in payload.Records)
            {
                if (string.IsNullOrWhiteSpace(item.ClientId))
                {
                    _logger.LogWarning("Skipping JSON record with missing clientId");
                    continue;
                }

                if (item.Value < 0)
                {
                    _logger.LogWarning("Skipping JSON record with negative value for client {ClientId}", item.ClientId);
                    continue;
                }

                records.Add(new FileRecord(item.ClientId, item.ProductType, item.Value));
            }

            _logger.LogInformation("Parsed {Count} records from JSON", records.Count);
            return Result<IReadOnlyList<FileRecord>>.Ok(records);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON file");
            return Result<IReadOnlyList<FileRecord>>.Fail(DomainErrors.File.ParseError);
        }
    }

    private record JsonPayload(
        [property: JsonPropertyName("referenceDate")] string? ReferenceDate,
        [property: JsonPropertyName("records")] List<JsonRecord>? Records
    );

    private record JsonRecord(
        [property: JsonPropertyName("clientId")] string? ClientId,
        [property: JsonPropertyName("productType")] ProductType ProductType,
        [property: JsonPropertyName("value")] decimal Value
    );
}
