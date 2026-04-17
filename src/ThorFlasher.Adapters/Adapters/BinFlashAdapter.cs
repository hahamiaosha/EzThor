using ThorFlasher.Adapters.Helpers;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Adapters.Adapters;

public sealed class BinFlashAdapter : IThorOperationAdapter
{
    private readonly IProcessRunner _processRunner;
    private readonly ThorCommandOptions _commandOptions;

    public BinFlashAdapter(IProcessRunner processRunner, ThorCommandOptions commandOptions)
    {
        _processRunner = processRunner;
        _commandOptions = commandOptions;
    }

    public bool CanHandle(string filePath)
    {
        return string.Equals(Path.GetExtension(filePath), ".bin", StringComparison.OrdinalIgnoreCase);
    }

    public Task<OperationResult> ExecuteAsync(
        OperationContext context,
        IProgress<LogEntry> progress,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(progress);

        // Replace this placeholder with the real THOR BIN flash command in appsettings.json.
        var definition = _commandOptions.BinFlash;
        var arguments = CommandTemplateExpander.Expand(definition.ArgumentsTemplate, context);

        progress.Report(new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "INFO",
            Stage = "BinFlash",
            Message = $"Starting BIN flash using executable '{definition.Executable}'."
        });

        progress.Report(new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = "INFO",
            Stage = "BinFlash",
            Message = "Replace this placeholder with the real THOR BIN flash command if the default configuration is still in use."
        });

        return _processRunner.RunAsync(
            new ProcessRunRequest
            {
                Executable = definition.Executable,
                Arguments = arguments,
                WorkingDirectory = ResolveWorkingDirectory(definition, context),
                Stage = "BinFlash"
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
