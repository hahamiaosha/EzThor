using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Interfaces;

public interface IScriptConfigurationUpdater
{
    Task ApplyIpSettingsAsync(OperationContext context, IProgress<LogEntry> progress, CancellationToken token);
}
