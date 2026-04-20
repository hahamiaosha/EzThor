using System.Windows.Media;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;
using ThorFlasher.Core.Services;
using ThorFlasher.UI.Services;
using ThorFlasher.UI.ViewModels;
using ThorFlasher.UI.Views;

namespace ThorFlasher.Core.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public void SettingBinFile_LogsFlashGuidanceWithManualRecovery()
    {
        var viewModel = CreateViewModel(out _, out _);
        var filePath = CreateTempFile(".bin");

        viewModel.FilePath = filePath;

        var guidance = Assert.Single(viewModel.Logs.Where(entry => entry.Stage == "Guidance"));
        Assert.Contains(".bin file", guidance.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Flash", guidance.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("recovery mode", guidance.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettingCapFile_LogsCapsuleUpdateGuidance()
    {
        var viewModel = CreateViewModel(out _, out _);
        var filePath = CreateTempFile(".cap");

        viewModel.FilePath = filePath;

        var guidance = Assert.Single(viewModel.Logs.Where(entry => entry.Stage == "Guidance"));
        Assert.Contains(".cap file", guidance.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Capsule Update", guidance.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettingSameFileTwice_DoesNotDuplicateGuidance()
    {
        var viewModel = CreateViewModel(out _, out _);
        var filePath = CreateTempFile(".bin");

        viewModel.FilePath = filePath;
        viewModel.FilePath = filePath;

        Assert.Single(viewModel.Logs.Where(entry => entry.Stage == "Guidance"));
    }

    [Fact]
    public void UnsupportedFile_DoesNotEmitGuidance()
    {
        var viewModel = CreateViewModel(out _, out _);
        var filePath = CreateTempFile(".txt");

        viewModel.FilePath = filePath;

        Assert.DoesNotContain(viewModel.Logs, entry => entry.Stage == "Guidance");
    }

    [Fact]
    public void CopyLogCommand_CopiesPlainTextOutput()
    {
        var viewModel = CreateViewModel(out var clipboardService, out _);
        var filePath = CreateTempFile(".bin");

        viewModel.FilePath = filePath;
        viewModel.CopyLogCommand.Execute(null);

        Assert.NotNull(clipboardService.LastText);
        Assert.Contains("[INFO] Guidance:", clipboardService.LastText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("manual", clipboardService.LastText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogEntryTextStyle_UsesRedForErrorLines()
    {
        var errorBrush = Assert.IsType<SolidColorBrush>(LogEntryTextStyle.GetForegroundBrush("ERROR"));
        var infoBrush = Assert.IsType<SolidColorBrush>(LogEntryTextStyle.GetForegroundBrush("INFO"));

        Assert.Equal(LogEntryTextStyle.ErrorColor, errorBrush.Color);
        Assert.Equal(LogEntryTextStyle.DefaultColor, infoBrush.Color);
        Assert.NotEqual(errorBrush.Color, infoBrush.Color);
    }

    private static MainViewModel CreateViewModel(out FakeClipboardService clipboardService, out FakeUserDialogService userDialogService)
    {
        clipboardService = new FakeClipboardService();
        userDialogService = new FakeUserDialogService();

        return new MainViewModel(
            new FileTypeResolver(),
            new InputValidator(new FileTypeResolver()),
            new FakeWorkflowOrchestrator(),
            new ThorScriptSettings(),
            new FakeProfileStore(),
            new FakeFileDialogService(),
            userDialogService,
            clipboardService);
    }

    private static string CreateTempFile(string extension)
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, $"package{extension}");
        File.WriteAllText(path, "test");
        return path;
    }

    private sealed class FakeWorkflowOrchestrator : IThorWorkflowOrchestrator
    {
        public Task<OperationResult> ExecuteAsync(OperationContext context, IProgress<LogEntry> progress, CancellationToken token)
        {
            return Task.FromResult(new OperationResult
            {
                Success = true,
                SummaryMessage = "Success",
                ExitCode = 0
            });
        }
    }

    private sealed class FakeProfileStore : IProfileStore
    {
        public Task SaveAsync(EnvironmentProfile profile, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EnvironmentProfile>> GetAllAsync(CancellationToken token = default)
        {
            return Task.FromResult<IReadOnlyList<EnvironmentProfile>>(Array.Empty<EnvironmentProfile>());
        }

        public Task<EnvironmentProfile?> LoadAsync(string profileName, CancellationToken token = default)
        {
            return Task.FromResult<EnvironmentProfile?>(null);
        }
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? BrowseForFirmwareFile()
        {
            return null;
        }
    }

    private sealed class FakeUserDialogService : IUserDialogService
    {
        public bool ConfirmOperation(OperationType operationType, string targetIp, string filePath)
        {
            return true;
        }

        public void ShowInformation(string title, string message)
        {
        }

        public void ShowError(string title, string message)
        {
        }

        public string? PromptForProfileName(string? initialName)
        {
            return initialName;
        }

        public EnvironmentProfile? PickProfile(IReadOnlyList<EnvironmentProfile> profiles)
        {
            return profiles.FirstOrDefault();
        }
    }

    private sealed class FakeClipboardService : IClipboardService
    {
        public string? LastText { get; private set; }

        public void SetText(string text)
        {
            LastText = text;
        }
    }
}
