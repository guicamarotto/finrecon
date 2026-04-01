using FinRecon.Core.Common;

namespace FinRecon.Core.Domain.Errors;

public static class DomainErrors
{
    public static class Job
    {
        public static readonly DomainError DuplicateFile = new(
            "job.duplicate_file",
            "A file with this hash already exists for the given reference date.");

        public static readonly DomainError NotFound = new(
            "job.not_found",
            "The reconciliation job was not found.");

        public static readonly DomainError InvalidTransition = new(
            "job.invalid_transition",
            "The job cannot transition to the requested state from its current status.");
    }

    public static class File
    {
        public static readonly DomainError InvalidFormat = new(
            "file.invalid_format",
            "The file format is not supported. Use CSV or JSON.");

        public static readonly DomainError ExceedsMaxSize = new(
            "file.exceeds_max_size",
            "File exceeds the 10MB limit.");

        public static readonly DomainError InvalidSchema = new(
            "file.invalid_schema",
            "File does not conform to the expected schema.");

        public static readonly DomainError ParseError = new(
            "file.parse_error",
            "The file could not be parsed. Check the format and try again.");
    }
}
