using ThorFlasher.Adapters.ScriptExecution;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;
using ThorFlasher.Core.Services;

namespace ThorFlasher.Core.Tests;

public sealed class ThorWorkflowOrchestratorTests
{
    [Fact]
    public async Task ExecuteAsync_RunsUploadThenFlash()
    {
        var runner = new FakeProcessRunner(successStages: ["Upload", "Flash"]);
        var orchestrator = CreateOrchestrator(runner);

        var result = await orchestrator.ExecuteAsync(CreateContext(OperationType.Flash), new Progress<LogEntry>(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(["Upload", "Flash"], runner.Stages);
    }

    [Fact]
    public async Task ExecuteAsync_RunsUploadThenCapsuleUpdate()
    {
        var runner = new FakeProcessRunner(successStages: ["Upload", "CapsuleUpdate"]);
        var orchestrator = CreateOrchestrator(runner);

        var result = await orchestrator.ExecuteAsync(CreateContext(OperationType.CapsuleUpdate), new Progress<LogEntry>(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(["Upload", "CapsuleUpdate"], runner.Stages);
    }

    [Fact]
    public async Task ExecuteAsync_StopsWhenUploadFails()
    {
        var runner = new FakeProcessRunner(successStages: [], failingStage: "Upload");
        var orchestrator = CreateOrchestrator(runner);

        var result = await orchestrator.ExecuteAsync(CreateContext(OperationType.Flash), new Progress<LogEntry>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(["Upload"], runner.Stages);
    }

    [Fact]
    public async Task ExecuteAsync_StopsWhenConfigurationUpdateFails()
    {
        var runner = new FakeProcessRunner(successStages: ["Upload", "Flash"]);
        var orchestrator = new ThorWorkflowOrchestrator(
            new InputValidator(new FileTypeResolver()),
            new FailingScriptConfigurationUpdater(),
            new FakeThorScriptResolver(),
            runner);

        var result = await orchestrator.ExecuteAsync(CreateContext(OperationType.Flash), new Progress<LogEntry>(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(runner.Stages);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCancelled_WhenCancellationRequested()
    {
        var runner = new FakeProcessRunner(successStages: ["Upload", "Flash"], cancelImmediately: true);
        var orchestrator = CreateOrchestrator(runner);

        var result = await orchestrator.ExecuteAsync(CreateContext(OperationType.Flash), new Progress<LogEntry>(), new CancellationToken(canceled: true));

        Assert.False(result.Success);
        Assert.Contains("cancel", result.SummaryMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static ThorWorkflowOrchestrator CreateOrchestrator(IProcessRunner runner)
    {
        return new ThorWorkflowOrchestrator(
            new InputValidator(new FileTypeResolver()),
            new PassThroughScriptConfigurationUpdater(),
            new FakeThorScriptResolver(),
            runner);
    }

    private static OperationContext CreateContext(OperationType operationType)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bin");
        File.WriteAllText(filePath, "test");

        return new OperationContext
        {
            HostIp = "10.0.0.1",
            TargetIp = "10.0.0.2",
            FilePath = filePath,
            OperationType = operationType,
            ScriptsRootPath = @"C:\repo\Thor_Script",
            WorkingDirectory = Path.GetTempPath()
        };
    }

    private sealed class PassThroughScriptConfigurationUpdater : IScriptConfigurationUpdater
    {
        public Task ApplyIpSettingsAsync(OperationContext context, IProgress<LogEntry> progress, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FailingScriptConfigurationUpdater : IScriptConfigurationUpdater
    {
        public Task ApplyIpSettingsAsync(OperationContext context, IProgress<LogEntry> progress, CancellationToken token)
        {
            throw new InvalidOperationException("Config update failed.");
        }
    }

    private sealed class FakeThorScriptResolver : IThorScriptResolver
    {
        public ScriptCommandDefinition GetSendBuildCommand(OperationContext context)
        {
            return new ScriptCommandDefinition
            {
                Enabled = true,
                Executable = "bash.exe",
                ArgumentsTemplate = "send_build.sh",
                WorkingDirectory = context.WorkingDirectory
            };
        }

        public ScriptCommandDefinition GetOperationCommand(OperationContext context)
        {
            return new ScriptCommandDefinition
            {
                Enabled = true,
                Executable = "bash.exe",
                ArgumentsTemplate = context.OperationType == OperationType.Flash ? "flash.sh" : "capsule.sh",
                WorkingDirectory = context.WorkingDirectory
            };
        }
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly HashSet<string> _successStages;
        private readonly string? _failingStage;
        private readonly bool _cancelImmediately;

        public FakeProcessRunner(IEnumerable<string> successStages, string? failingStage = null, bool cancelImmediately = false)
        {
            _successStages = new HashSet<string>(successStages, StringComparer.OrdinalIgnoreCase);
            _failingStage = failingStage;
            _cancelImmediately = cancelImmediately;
        }

        public List<string> Stages { get; } = [];

        public Task<OperationResult> RunAsync(ProcessRunRequest request, IProgress<LogEntry> progress, CancellationToken token)
        {
            if (_cancelImmediately || token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }

            var stage = request.EnvironmentVariables?["THORFLASHER_LOG_STAGE"] ?? "Process";
            Stages.Add(stage);

            if (string.Equals(stage, _failingStage, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new OperationResult
                {
                    Success = false,
                    ExitCode = 1,
                    SummaryMessage = $"{stage} failed."
                });
            }

            return Task.FromResult(new OperationResult
            {
                Success = _successStages.Contains(stage),
                ExitCode = 0,
                SummaryMessage = $"{stage} succeeded."
            });
        }
    }
}
