using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;
using System.Text.RegularExpressions;

namespace ThorFlasher.Infrastructure.Runtime;

public sealed class ScriptConfigurationUpdater : IScriptConfigurationUpdater
{
    private readonly ThorScriptSettings _thorScriptSettings;

    public ScriptConfigurationUpdater(ThorScriptSettings thorScriptSettings)
    {
        _thorScriptSettings = thorScriptSettings;
    }

    public async Task ApplyIpSettingsAsync(OperationContext context, IProgress<LogEntry> progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(progress);

        if (string.IsNullOrWhiteSpace(_thorScriptSettings.IpConfigFileRelativePath))
        {
            throw new InvalidOperationException("ThorScriptSettings:IpConfigFileRelativePath is not configured.");
        }

        var scriptsRoot = ResolveScriptsRoot(context.ScriptsRootPath);
        if (!Directory.Exists(scriptsRoot))
        {
            throw new DirectoryNotFoundException($"Scripts root path '{scriptsRoot}' does not exist.");
        }

        var configPath = Path.GetFullPath(Path.Combine(scriptsRoot, _thorScriptSettings.IpConfigFileRelativePath));
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Script configuration file '{configPath}' was not found.", configPath);
        }

        progress.Report(CreateLog("INFO", "ScriptConfig", $"Updating script configuration file '{configPath}'."));

        var originalContent = await File.ReadAllTextAsync(configPath, token).ConfigureAwait(false);
        var updatedContent = originalContent;

        updatedContent = ReplaceTokenOrAssignment(updatedContent, _thorScriptSettings.HostIpToken, "THOR_HOST_IP", context.HostIp, "Host IP");
        updatedContent = ReplaceTokenOrAssignment(updatedContent, _thorScriptSettings.TargetIpToken, "THOR_TARGET_IP", context.TargetIp, "Target IP");

        if (!string.IsNullOrWhiteSpace(_thorScriptSettings.SelectedFileToken))
        {
            updatedContent = ReplaceTokenOrAssignment(updatedContent, _thorScriptSettings.SelectedFileToken, "SELECTED_FILE_PATH", context.FilePath, "Selected file path");
        }

        updatedContent = ReplaceTokenOrAssignment(updatedContent, _thorScriptSettings.HostUserToken, "REMOTE_USER", context.HostUser, "Host user");
        updatedContent = ReplaceTokenOrAssignment(updatedContent, _thorScriptSettings.HostPasswordToken, "REMOTE_PASS", context.HostPassword, "Host password");
        updatedContent = ReplaceTokenOrAssignment(updatedContent, _thorScriptSettings.TargetUserToken, "TARGET_USER", context.TargetUser, "Target user");
        updatedContent = ReplaceTokenOrAssignment(updatedContent, _thorScriptSettings.TargetPasswordToken, "TARGET_PASS", context.TargetPassword, "Target password");

        var backupPath = $"{configPath}.bak";
        await File.WriteAllTextAsync(backupPath, originalContent, token).ConfigureAwait(false);
        progress.Report(CreateLog("INFO", "ScriptConfig", $"Created backup file '{backupPath}'."));

        await File.WriteAllTextAsync(configPath, updatedContent, token).ConfigureAwait(false);
        progress.Report(CreateLog("INFO", "ScriptConfig", "Script configuration update complete."));
    }

    private static string ResolveScriptsRoot(string scriptsRootPath)
    {
        return Path.IsPathRooted(scriptsRootPath)
            ? Path.GetFullPath(scriptsRootPath)
            : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, scriptsRootPath));
    }

    private static string ReplaceTokenOrAssignment(string content, string? token, string variableName, string replacement, string label)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException($"{label} token is not configured.");
        }

        if (content.Contains(token, StringComparison.Ordinal))
        {
            return content.Replace(token, replacement, StringComparison.Ordinal);
        }

        var updatedAssignment = ReplaceAssignmentValue(content, variableName, replacement);
        if (updatedAssignment is not null)
        {
            return updatedAssignment;
        }

        throw new InvalidOperationException(
            $"{label} token '{token}' was not found, and assignment '{variableName}=...' was also not found in the configured script file.");
    }

    private static string? ReplaceAssignmentValue(string content, string variableName, string replacement)
    {
        var pattern = $@"^(?<prefix>\s*{Regex.Escape(variableName)}\s*=\s*)(?<value>.*)$";
        var replacementValue = QuoteForShell(replacement);
        var regex = new Regex(pattern, RegexOptions.Multiline);

        if (!regex.IsMatch(content))
        {
            return null;
        }

        return regex.Replace(
            content,
            match => $"{match.Groups["prefix"].Value}{replacementValue}",
            1);
    }

    private static string QuoteForShell(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("$", "\\$", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);

        return $"\"{escaped}\"";
    }

    private static LogEntry CreateLog(string level, string stage, string message)
    {
        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Stage = stage,
            Message = message
        };
    }
}
