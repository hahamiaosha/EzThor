namespace ThorFlasher.Core.Models;

public sealed class EnvironmentProfile
{
    public string ProfileName { get; set; } = string.Empty;

    public string HostIp { get; set; } = string.Empty;

    public string TargetIp { get; set; } = string.Empty;

    public string LastFilePath { get; set; } = string.Empty;

    public OperationType LastOperation { get; set; }

    public DateTime UpdatedAt { get; set; }
}
