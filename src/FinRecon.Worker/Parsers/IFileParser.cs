using FinRecon.Core.Common;
using FinRecon.Core.Domain.ValueObjects;

namespace FinRecon.Worker.Parsers;

public interface IFileParser
{
    bool CanParse(string filename);
    Task<Result<IReadOnlyList<FileRecord>>> ParseAsync(Stream content, CancellationToken ct = default);
}
