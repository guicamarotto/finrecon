using FinRecon.API.DTOs;
using FluentValidation;

namespace FinRecon.API.Validators;

public class UploadReconciliationRequestValidator : AbstractValidator<UploadReconciliationRequest>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csv", ".json"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/csv", "application/csv", "application/json", "text/plain"
    };

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

    public UploadReconciliationRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("A file is required.");

        When(x => x.File != null, () =>
        {
            RuleFor(x => x.File.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage($"File must not exceed 50MB.");

            RuleFor(x => x.File.FileName)
                .Must(HasAllowedExtension)
                .WithMessage("Only .csv and .json files are accepted.");

            RuleFor(x => x.File.ContentType)
                .Must(HasAllowedMimeType)
                .WithMessage("Unsupported content type. Use text/csv or application/json.");
        });

        RuleFor(x => x.ReferenceDate)
            .NotEmpty().WithMessage("Reference date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Reference date cannot be in the future.");
    }

    private static bool HasAllowedExtension(string filename)
    {
        var ext = Path.GetExtension(filename);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    private static bool HasAllowedMimeType(string contentType)
        => AllowedMimeTypes.Contains(contentType);
}
