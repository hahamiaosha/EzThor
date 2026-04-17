using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Interfaces;

public interface IThorWorkflowOrchestrator
{
    Task<OperationResult> ExecuteAsync(OperationContext context, IProgress<LogEntry> progress, CancellationToken token);
}
