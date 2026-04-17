using ThorFlasher.Adapters.Helpers;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Adapters.ScriptExecution;

public sealed class ThorScriptResolver : IThorScriptResolver
{
    private readonly ThorScriptSettings _thorScriptSettings;
    private readonly ScriptExecutionSettings _scriptExecutionSettings;

    public ThorScriptResolver(ThorScriptSettings thorScriptSettings, ScriptExecutionSettings scriptExecutionSettings)
    {
        _thorScriptSettings = thorScriptSettings;
        _scriptExecutionSettings = scriptExecutionSettings;
    }

    public ScriptCommandDefinition GetSendBuildCommand(OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scriptPath = ResolveScriptPath(context.ScriptsRootPath, _thorScriptSettings.SendBuildScriptRelativePath);
        var scriptArguments = ShellTemplateExpander.ExpandScriptArguments(_scriptExecutionSettings.SendBuildArgumentsTemplate, context);

        return CreateShellDefinition(scriptPath, scriptArguments, context.WorkingDirectory);
    }

    public ScriptCommandDefinition GetOperationCommand(OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.OperationType switch
        {
            OperationType.Flash => CreateShellDefinition(
                ResolveScriptPath(context.ScriptsRootPath, _thorScriptSettings.FlashScriptRelativePath),
                ShellTemplateExpander.ExpandScriptArguments(_scriptExecutionSettings.FlashArgumentsTemplate, context),
                context.WorkingDirectory),
            OperationType.CapsuleUpdate => CreateShellDefinition(
                ResolveScriptPath(context.ScriptsRootPath, _thorScriptSettings.CapsuleScriptRelativePath),
                ShellTemplateExpander.ExpandScriptArguments(_scriptExecutionSettings.CapsuleArgumentsTemplate, context),
                context.WorkingDirectory),
            _ => throw new InvalidOperationException($"Unsupported operation type '{context.OperationType}'.")
        };
    }

    private ScriptCommandDefinition CreateShellDefinition(string scriptPath, string scriptArguments, string workingDirectory)
    {
        var translatedWorkingDirectory = ScriptPathTranslator.TranslateIfNeeded(
            workingDirectory,
            _scriptExecutionSettings.TranslateWindowsPathsForWsl,
            _scriptExecutionSettings.ShellExecutable);
        var shellScriptPath = ResolveShellScriptPath(scriptPath, workingDirectory);
        var translatedScriptPath = ScriptPathTranslator.TranslateIfNeeded(
            shellScriptPath,
            _scriptExecutionSettings.TranslateWindowsPathsForWsl,
            _scriptExecutionSettings.ShellExecutable);

        return new ScriptCommandDefinition
        {
            Enabled = true,
            Executable = _scriptExecutionSettings.ShellExecutable,
            ArgumentsTemplate = ShellTemplateExpander.BuildShellArguments(
                _scriptExecutionSettings.ShellArgumentsTemplate,
                translatedScriptPath,
                scriptArguments,
                translatedWorkingDirectory),
            WorkingDirectory = workingDirectory
        };
    }

    private string ResolveShellScriptPath(string scriptPath, string workingDirectory)
    {
        if (UsesBashShell(_scriptExecutionSettings.ShellExecutable))
        {
            var scriptDirectory = Path.GetDirectoryName(scriptPath);
            if (!string.IsNullOrWhiteSpace(scriptDirectory)
                && PathsEqual(scriptDirectory, workingDirectory))
            {
                return $"./{Path.GetFileName(scriptPath)}";
            }
        }

        return scriptPath;
    }

    private static bool UsesBashShell(string? shellExecutable)
    {
        if (string.IsNullOrWhiteSpace(shellExecutable))
        {
            return false;
        }

        var fileName = Path.GetFileName(shellExecutable);
        return fileName.Equals("bash.exe", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("bash", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("sh.exe", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("sh", StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static string ResolveScriptPath(string scriptsRootPath, string relativeScriptPath)
    {
        string fullPath;

        if (Path.IsPathRooted(relativeScriptPath))
        {
            fullPath = Path.GetFullPath(relativeScriptPath);
        }
        else
        {
            var basePath = Path.IsPathRooted(scriptsRootPath)
                ? scriptsRootPath
                : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, scriptsRootPath));

            fullPath = Path.GetFullPath(Path.Combine(basePath, relativeScriptPath));
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Configured script file '{fullPath}' was not found.", fullPath);
        }

        return fullPath;
    }
}
