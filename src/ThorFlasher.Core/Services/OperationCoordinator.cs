using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Services;

public sealed class OperationCoordinator
{
    private readonly IReadOnlyList<IThorOperationAdapter> _adapters;
    private readonly FileTypeResolver _fileTypeResolver;

    public OperationCoordinator(IEnumerable<IThorOperationAdapter> adapters, FileTypeResolver fileTypeResolver)
    {
        _adapters = adapters.ToArray();
        _fileTypeResolver = fileTypeResolver;
    }

    public async Task<OperationResult> ExecuteAsync(
        OperationContext context,
        IProgress<LogEntry> progress,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(progress);

        var operationType = _fileTypeResolver.Resolve(context.FilePath);
        if (operationType == OperationType.Unknown)
        {
            progress.Report(CreateErrorLog("Coordinator", "No supported operation could be determined from the selected file."));
            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = "Unsupported file type."
            };
        }

        var adapter = _adapters.FirstOrDefault(candidate => candidate.CanHandle(context.FilePath));
        if (adapter is null)
        {
            progress.Report(CreateErrorLog("Coordinator", $"No adapter is registered for {operationType}."));
            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = $"No adapter is available for {operationType}."
            };
        }

        progress.Report(new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "INFO",
            Stage = "Coordinator",
            Message = $"Dispatching {operationType}."
        });

        return await adapter.ExecuteAsync(context, progress, token).ConfigureAwait(false);
    }

    private static LogEntry CreateErrorLog(string stage, string message)
    {
        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "ERROR",
            Stage = stage,
            Message = message
        };
    }
}
