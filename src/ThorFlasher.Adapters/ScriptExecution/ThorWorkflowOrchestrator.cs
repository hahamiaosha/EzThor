using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;
using ThorFlasher.Core.Services;

namespace ThorFlasher.Adapters.ScriptExecution;

public sealed class ThorWorkflowOrchestrator : IThorWorkflowOrchestrator
{
    private const string StageTokenKey = "THORFLASHER_LOG_STAGE";

    private readonly InputValidator _inputValidator;
    private readonly IScriptConfigurationUpdater _scriptConfigurationUpdater;
    private readonly IThorScriptResolver _scriptResolver;
    private readonly IProcessRunner _processRunner;

    public ThorWorkflowOrchestrator(
        InputValidator inputValidator,
        IScriptConfigurationUpdater scriptConfigurationUpdater,
        IThorScriptResolver scriptResolver,
        IProcessRunner processRunner)
    {
        _inputValidator = inputValidator;
        _scriptConfigurationUpdater = scriptConfigurationUpdater;
        _scriptResolver = scriptResolver;
        _processRunner = processRunner;
    }

    public async Task<OperationResult> ExecuteAsync(OperationContext context, IProgress<LogEntry> progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(progress);

        progress.Report(CreateLog("INFO", "Validation", "Validating user input."));
        var validationResult = _inputValidator.Validate(context);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                progress.Report(CreateLog("ERROR", "Validation", error));
            }

            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = "Validation failed."
            };
        }

        try
        {
            token.ThrowIfCancellationRequested();

            progress.Report(CreateLog("INFO", "ScriptConfig", "Applying script-side IP and file configuration."));
            await _scriptConfigurationUpdater.ApplyIpSettingsAsync(context, progress, token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            var uploadDefinition = _scriptResolver.GetSendBuildCommand(context);
            if (uploadDefinition.Enabled)
            {
                var uploadResult = await RunDefinitionAsync(uploadDefinition, "Upload", progress, token).ConfigureAwait(false);
                if (!uploadResult.Success)
                {
                    progress.Report(CreateLog("ERROR", "Completion", "Upload failed. Operation script will not run."));
                    return uploadResult;
                }
            }
            else
            {
                progress.Report(CreateLog("INFO", "Upload", "Upload step skipped (not required for this operation)."));
            }

            token.ThrowIfCancellationRequested();

            var operationStage = context.OperationType == OperationType.Flash ? "Flash" : "CapsuleUpdate";
            var operationDefinition = _scriptResolver.GetOperationCommand(context);
            var operationResult = await RunDefinitionAsync(operationDefinition, operationStage, progress, token).ConfigureAwait(false);

            progress.Report(CreateLog(operationResult.Success ? "INFO" : "ERROR", "Completion", operationResult.SummaryMessage));
            return operationResult;
        }
        catch (OperationCanceledException)
        {
            progress.Report(CreateLog("WARN", "Completion", "Operation cancelled."));
            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = "Operation cancelled."
            };
        }
        catch (Exception exception)
        {
            progress.Report(CreateLog("ERROR", "Completion", exception.Message));
            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = "Workflow failed.",
                Exception = exception
            };
        }
    }

    private async Task<OperationResult> RunDefinitionAsync(
        ScriptCommandDefinition definition,
        string stage,
        IProgress<LogEntry> progress,
        CancellationToken token)
    {
        if (!definition.Enabled)
        {
            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = $"{stage} command is disabled."
            };
        }

        progress.Report(CreateLog("INFO", stage, $"Starting {stage} command with '{definition.Executable}'."));

        return await _processRunner.RunAsync(
            new ProcessRunRequest
            {
                FileName = definition.Executable,
                Arguments = definition.ArgumentsTemplate,
                WorkingDirectory = definition.WorkingDirectory,
                EnvironmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [StageTokenKey] = stage
                }
            },
            progress,
            token).ConfigureAwait(false);
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
