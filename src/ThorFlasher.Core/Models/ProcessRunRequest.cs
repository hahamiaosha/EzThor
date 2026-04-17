namespace ThorFlasher.Core.Models;

public sealed class ProcessRunRequest
{
    public string Executable { get; set; } = string.Empty;

    public string Arguments { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;
}
