using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;
using ThorFlasher.Core.Services;

namespace ThorFlasher.Core.Tests;

public sealed class OperationCoordinatorTests
{
    private readonly FileTypeResolver _resolver = new();

    [Theory]
    [InlineData("firmware.bin", OperationType.BinFlash)]
    [InlineData("capsule.cap", OperationType.CapUpdate)]
    public async Task ExecuteAsync_UsesMatchingAdapter(string filePath, OperationType expectedOperation)
    {
        var adapter = new FakeAdapter(expectedOperation, success: true);
        var coordinator = new OperationCoordinator([adapter], _resolver);

        var result = await coordinator.ExecuteAsync(
            new OperationContext
            {
                HostIp = "192.168.1.10",
                TargetIp = "192.168.1.20",
                FilePath = filePath,
                WorkingDirectory = Environment.CurrentDirectory
            },
            new Progress<LogEntry>(),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, adapter.ExecutionCount);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenNoAdapterCanHandleFile()
    {
        var coordinator = new OperationCoordinator([], _resolver);

        var result = await coordinator.ExecuteAsync(
            new OperationContext
            {
                HostIp = "192.168.1.10",
                TargetIp = "192.168.1.20",
                FilePath = "firmware.bin",
                WorkingDirectory = Environment.CurrentDirectory
            },
            new Progress<LogEntry>(),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("adapter", result.SummaryMessage, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeAdapter : IThorOperationAdapter
    {
        private readonly OperationType _operationType;
        private readonly bool _success;

        public FakeAdapter(OperationType operationType, bool success)
        {
            _operationType = operationType;
            _success = success;
        }

        public int ExecutionCount { get; private set; }

        public bool CanHandle(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return _operationType switch
            {
                OperationType.BinFlash => string.Equals(extension, ".bin", StringComparison.OrdinalIgnoreCase),
                OperationType.CapUpdate => string.Equals(extension, ".cap", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        public Task<OperationResult> ExecuteAsync(OperationContext context, IProgress<LogEntry> progress, CancellationToken token)
        {
            ExecutionCount++;

            return Task.FromResult(new OperationResult
            {
                Success = _success,
                ExitCode = _success ? 0 : 1,
                SummaryMessage = _success ? "Success" : "Failure"
            });
        }
    }
}
