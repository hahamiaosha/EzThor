namespace ThorFlasher.Core.Models;

public sealed class OperationResult
{
    public bool Success { get; set; }

    public string SummaryMessage { get; set; } = string.Empty;

    public int ExitCode { get; set; }

    public Exception? Exception { get; set; }
}
