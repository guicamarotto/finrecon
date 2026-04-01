namespace FinRecon.Core.Services;

public static class ReconciliationConstants
{
    /// <summary>
    /// Maximum absolute difference between current and previous values
    /// for records to be considered matched. Uses decimal to avoid
    /// floating-point precision issues in financial calculations.
    /// </summary>
    public const decimal MatchTolerance = 0.01m;
}
