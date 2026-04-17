using ThorFlasher.Adapters.Helpers;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Adapters.Adapters;

public sealed class CapUpdateAdapter : IThorOperationAdapter
{
    private readonly IProcessRunner _processRunner;
    private readonly ThorCommandOptions _commandOptions;

    public CapUpdateAdapter(IProcessRunner processRunner, ThorCommandOptions commandOptions)
    {
        _processRunner = processRunner;
        _commandOptions = commandOptions;
    }

    public bool CanHandle(string filePath)
    {
        return string.Equals(Path.GetExtension(filePath), ".cap", StringComparison.OrdinalIgnoreCase);
    }

    public Task<OperationResult> ExecuteAsync(
        OperationContext context,
        IProgress<LogEntry> progress,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(progress);

        // Replace this placeholder with the real THOR CAP update command in appsettings.json.
        var definition = _commandOptions.CapUpdate;
        var arguments = CommandTemplateExpander.Expand(definition.ArgumentsTemplate, context);

        progress.Report(new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "INFO",
            Stage = "CapUpdate",
            Message = $"Starting CAP update using executable '{definition.Executable}'."
        });

        progress.Report(new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "INFO",
            Stage = "CapUpdate",
            Message = "Replace this placeholder with the real THOR CAP update command if the default configuration is still in use."
        });

        return _processRunner.RunAsync(
            new ProcessRunRequest
            {
                Executable = definition.Executable,
                Arguments = arguments,
                WorkingDirectory = ResolveWorkingDirectory(definition, context),
                Stage = "CapUpdate"
            },
            progress,
            token);
    }

    private static string ResolveWorkingDirectory(OperationCommandDefinition definition, OperationContext context)
    {
        if (!string.IsNullOrWhiteSpace(definition.WorkingDirectory))
        {
            return definition.WorkingDirectory;
        }

        return !string.IsNullOrWhiteSpace(context.WorkingDirectory)
            ? context.WorkingDirectory
            : Path.GetDirectoryName(context.FilePath) ?? Environment.CurrentDirectory;
    }
}
