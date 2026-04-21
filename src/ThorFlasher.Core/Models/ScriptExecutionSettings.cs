namespace ThorFlasher.Core.Models;

public sealed class ScriptExecutionSettings
{
    public string ShellExecutable { get; set; } = "bash.exe";

    public string ShellArgumentsTemplate { get; set; } = "{scriptPath} {args}";

    public string SendBuildArgumentsTemplate { get; set; } = string.Empty;

    public string FlashArgumentsTemplate { get; set; } = string.Empty;

    public string CapsuleArgumentsTemplate { get; set; } = string.Empty;

    public bool TranslateWindowsPathsForWsl { get; set; }
}
