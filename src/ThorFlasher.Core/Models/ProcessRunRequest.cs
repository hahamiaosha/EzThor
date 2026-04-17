namespace ThorFlasher.Core.Models;

public sealed class ProcessRunRequest
{
    public string FileName { get; set; } = string.Empty;

    public string Arguments { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;

    public IDictionary<string, string>? EnvironmentVariables { get; set; }
}
