using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Interfaces;

public interface IThorOperationAdapter
{
    bool CanHandle(string filePath);

    Task<OperationResult> ExecuteAsync(
        OperationContext context,
        IProgress<LogEntry> progress,
        CancellationToken token);
}
