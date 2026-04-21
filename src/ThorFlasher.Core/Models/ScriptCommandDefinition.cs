namespace ThorFlasher.Core.Models;

public sealed class ScriptCommandDefinition
{
    public string Executable { get; set; } = string.Empty;

    public string ArgumentsTemplate { get; set; } = string.Empty;

    public string WorkingDirectory { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}
