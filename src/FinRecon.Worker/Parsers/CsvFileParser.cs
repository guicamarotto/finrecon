using CsvHelper;
using CsvHelper.Configuration;
using FinRecon.Core.Common;
using FinRecon.Core.Domain.Enums;
using FinRecon.Core.Domain.Errors;
using FinRecon.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FinRecon.Worker.Parsers;

public class CsvFileParser : IFileParser
{
    private readonly ILogger<CsvFileParser> _logger;

    public CsvFileParser(ILogger<CsvFileParser> logger) => _logger = logger;

    public bool CanParse(string filename)
        => filename.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<Result<IReadOnlyList<FileRecord>>> ParseAsync(Stream content, CancellationToken ct = default)
    {
        try
        {
            using var reader = new StreamReader(content, leaveOpen: true);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,         // Don't throw on missing headers
                MissingFieldFound = null,       // Don't throw on missing fields
                PrepareHeaderForMatch = args => args.Header.ToLowerInvariant().Trim(),
                IgnoreBlankLines = true,
            };

            using var csv = new CsvReader(reader, config);
            await csv.ReadAsync();
            csv.ReadHeader();

            var records = new List<FileRecord>();
            while (await csv.ReadAsync())
            {
                if (ct.IsCancellationRequested) break;

                var clientId = csv.GetField<string>("client_id");
                var productTypeStr = csv.GetField<string>("product_type");
                var valueStr = csv.GetField<string>("value");

                if (string.IsNullOrWhiteSpace(clientId) ||
                    string.IsNullOrWhiteSpace(productTypeStr) ||
                    string.IsNullOrWhiteSpace(valueStr))
                {
                    _logger.LogWarning("Skipping CSV row {RowIndex} with missing required fields", csv.Parser.Row);
                    continue;
                }

                if (!Enum.TryParse<ProductType>(productTypeStr, ignoreCase: true, out var productType))
                {
                    _logger.LogWarning("Invalid product_type '{Value}' at row {Row}", productTypeStr, csv.Parser.Row);
                    continue;
                }

                if (!decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) || value < 0)
                {
                    _logger.LogWarning("Invalid value '{Value}' at row {Row}", valueStr, csv.Parser.Row);
                    continue;
                }

                records.Add(new FileRecord(clientId!, productType, value));
            }

            _logger.LogInformation("Parsed {Count} records from CSV", records.Count);
            return Result<IReadOnlyList<FileRecord>>.Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse CSV file");
            return Result<IReadOnlyList<FileRecord>>.Fail(DomainErrors.File.ParseError);
        }
    }
}
