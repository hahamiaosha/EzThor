using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Interfaces;

public interface ILogStore
{
    Task SaveLogAsync(string logName, IReadOnlyList<LogEntry> entries, CancellationToken token = default);

    Task<IReadOnlyList<LogEntry>> LoadLogAsync(string logName, CancellationToken token = default);

    Task<IReadOnlyList<string>> ListLogsAsync(CancellationToken token = default);
}
