using System.Text;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Adapters.Helpers;

internal static class CommandTemplateExpander
{
    public static string Expand(string template, OperationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(context);

        return template
            .Replace("{hostIp}", EscapeWindowsArgument(context.HostIp), StringComparison.Ordinal)
            .Replace("{targetIp}", EscapeWindowsArgument(context.TargetIp), StringComparison.Ordinal)
            .Replace("{filePath}", EscapeWindowsArgument(context.FilePath), StringComparison.Ordinal);
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
