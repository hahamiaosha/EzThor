using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Services;

public sealed class ConnectivityValidator
{
    public Task<bool> ValidateAsync(
        OperationContext context,
        IProgress<LogEntry>? progress = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        token.ThrowIfCancellationRequested();

        progress?.Report(new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "INFO",
            Stage = "Connectivity",
            Message = "Connectivity pre-check is currently a placeholder and has been skipped."
        });

        return Task.FromResult(true);
    }
}
