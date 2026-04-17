namespace ThorFlasher.Core.Models;

public sealed class OperationCommandDefinition
{
    public string Executable { get; set; } = string.Empty;

    public string ArgumentsTemplate { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;
}
