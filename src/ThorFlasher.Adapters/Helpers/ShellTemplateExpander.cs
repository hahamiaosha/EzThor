using System.Text;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Adapters.Helpers;

internal static class ShellTemplateExpander
{
    public static string BuildShellArguments(
        string shellArgumentsTemplate,
        string scriptPath,
        string scriptArguments,
        string? workingDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shellArgumentsTemplate);

        return shellArgumentsTemplate
            .Replace("{scriptPath}", EscapeWindowsArgument(scriptPath), StringComparison.Ordinal)
            .Replace("{args}", scriptArguments ?? string.Empty, StringComparison.Ordinal)
            .Replace("{workingDirectory}", EscapeWindowsArgument(workingDirectory ?? string.Empty), StringComparison.Ordinal);
    }

    public static string ExpandScriptArguments(string? template, OperationContext context)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        return template
            .Replace("{hostIp}", EscapeWindowsArgument(context.HostIp), StringComparison.Ordinal)
            .Replace("{targetIp}", EscapeWindowsArgument(context.TargetIp), StringComparison.Ordinal)
            .Replace("{filePath}", EscapeWindowsArgument(context.FilePath), StringComparison.Ordinal)
            .Replace("{scriptsRootPath}", EscapeWindowsArgument(context.ScriptsRootPath), StringComparison.Ordinal)
            .Replace("{hostUser}", EscapeWindowsArgument(context.HostUser), StringComparison.Ordinal)
            .Replace("{hostPassword}", EscapeWindowsArgument(context.HostPassword), StringComparison.Ordinal)
            .Replace("{targetUser}", EscapeWindowsArgument(context.TargetUser), StringComparison.Ordinal)
            .Replace("{targetPassword}", EscapeWindowsArgument(context.TargetPassword), StringComparison.Ordinal)
            .Replace("{flashMode}", context.FlashMode.ToString().ToLowerInvariant(), StringComparison.Ordinal)
            .Replace("{flashSlot}", context.FlashSlot.ToString(), StringComparison.Ordinal);
    }

    private static string EscapeWindowsArgument(string value)
    {
        value ??= string.Empty;

        if (value.Length == 0)
        {
            return "\"\"";
        }

        var builder = new StringBuilder();
        builder.Append('"');

        var backslashCount = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', (backslashCount * 2) + 1);
                builder.Append('"');
                backslashCount = 0;
                continue;
            }

            if (backslashCount > 0)
            {
                builder.Append('\\', backslashCount);
                backslashCount = 0;
            }

            builder.Append(character);
        }

        if (backslashCount > 0)
        {
            builder.Append('\\', backslashCount * 2);
        }

        builder.Append('"');
        return builder.ToString();
    }
}
