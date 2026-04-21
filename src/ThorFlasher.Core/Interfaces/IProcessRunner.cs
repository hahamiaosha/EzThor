using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Interfaces;

public interface IProcessRunner
{
    Task<OperationResult> RunAsync(ProcessRunRequest request, IProgress<LogEntry> progress, CancellationToken token);
}
