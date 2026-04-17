namespace ThorFlasher.Core.Models;

public sealed class OperationContext
{
    public string HostIp { get; set; } = string.Empty;

    public string TargetIp { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public OperationType OperationType { get; set; }

    public string ScriptsRootPath { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;
}
