using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Threading;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;
using ThorFlasher.Core.Services;
using ThorFlasher.UI.Commands;
using ThorFlasher.UI.Services;

namespace ThorFlasher.UI.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly FileTypeResolver _fileTypeResolver;
    private readonly InputValidator _inputValidator;
    private readonly IThorWorkflowOrchestrator _thorWorkflowOrchestrator;
    private readonly ThorScriptSettings _thorScriptSettings;
    private readonly IProfileStore _profileStore;
    private readonly IFileDialogService _fileDialogService;
    private readonly IUserDialogService _userDialogService;
    private readonly IClipboardService _clipboardService;
    private readonly Dispatcher _dispatcher;

    private string _hostIp = string.Empty;
    private string _targetIp = string.Empty;
    private string _hostUser = string.Empty;
    private string _hostPassword = string.Empty;
    private string _targetUser = string.Empty;
    private string _targetPassword = string.Empty;
    private string _filePath = string.Empty;
    private string _statusMessage = "Idle";
    private string _selectedOperationText = "Not started";
    private string _unhandledErrorMessage = string.Empty;
    private string _logText = string.Empty;
    private string _currentProfileName = string.Empty;
    private bool _isRunning;
    private ValidationResult _validationResult = ValidationResult.Failure("Enter THOR Host IP, THOR Target IP, and a valid file.");
    private CancellationTokenSource? _currentOperationCts;
    private OperationType _lastSelectedOperation = OperationType.None;
    private FlashMode _selectedFlashMode = FlashMode.Qspi;
    private FlashSlot _selectedFlashSlot = FlashSlot.A;

    public MainViewModel(
        FileTypeResolver fileTypeResolver,
        InputValidator inputValidator,
        IThorWorkflowOrchestrator thorWorkflowOrchestrator,
        ThorScriptSettings thorScriptSettings,
        IProfileStore profileStore,
        IFileDialogService fileDialogService,
        IUserDialogService userDialogService,
        IClipboardService clipboardService)
    {
        _fileTypeResolver = fileTypeResolver;
        _inputValidator = inputValidator;
        _thorWorkflowOrchestrator = thorWorkflowOrchestrator;
        _thorScriptSettings = thorScriptSettings;
        _profileStore = profileStore;
        _fileDialogService = fileDialogService;
        _userDialogService = userDialogService;
        _clipboardService = clipboardService;
        _dispatcher = Dispatcher.CurrentDispatcher;

        Logs = new ObservableCollection<LogEntry>();

        BrowseCommand = new RelayCommand(BrowseForFile, () => !IsRunning);
        SaveEnvironmentCommand = new AsyncRelayCommand(SaveEnvironmentAsync, () => !IsRunning, OnCommandException);
        LoadEnvironmentCommand = new AsyncRelayCommand(LoadEnvironmentAsync, () => !IsRunning, OnCommandException);
        FlashCommand = new AsyncRelayCommand(() => ExecuteOperationAsync(OperationType.Flash), CanStartOperation, OnCommandException);
        CapsuleUpdateCommand = new AsyncRelayCommand(() => ExecuteOperationAsync(OperationType.CapsuleUpdate), CanStartOperation, OnCommandException);
        CancelCommand = new RelayCommand(Cancel, () => IsRunning);
        CopyLogCommand = new RelayCommand(CopyLog, CanCopyLog);
        ClearLogCommand = new RelayCommand(ClearLogs);

        UpdateValidationState();
    }

    public ObservableCollection<LogEntry> Logs { get; }

    public RelayCommand BrowseCommand { get; }

    public AsyncRelayCommand SaveEnvironmentCommand { get; }

    public AsyncRelayCommand LoadEnvironmentCommand { get; }

    public AsyncRelayCommand FlashCommand { get; }

    public AsyncRelayCommand CapsuleUpdateCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand CopyLogCommand { get; }

    public RelayCommand ClearLogCommand { get; }

    public string HostIp
    {
        get => _hostIp;
        set
        {
            if (SetProperty(ref _hostIp, value))
            {
                UpdateValidationState();
            }
        }
    }

    public string TargetIp
    {
        get => _targetIp;
        set
        {
            if (SetProperty(ref _targetIp, value))
            {
                UpdateValidationState();
            }
        }
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                UpdateValidationState();
            }
        }
    }

    public string HostUser
    {
        get => _hostUser;
        set => SetProperty(ref _hostUser, value);
    }

    public string HostPassword
    {
        get => _hostPassword;
        set => SetProperty(ref _hostPassword, value);
    }

    public string TargetUser
    {
        get => _targetUser;
        set => SetProperty(ref _targetUser, value);
    }

    public string TargetPassword
    {
        get => _targetPassword;
        set => SetProperty(ref _targetPassword, value);
    }

    public FlashMode[] FlashModeOptions { get; } = [FlashMode.Qspi, FlashMode.Uefi, FlashMode.Bpmp];

    public FlashSlot[] FlashSlotOptions { get; } = [FlashSlot.A, FlashSlot.B];

    public FlashMode SelectedFlashMode
    {
        get => _selectedFlashMode;
        set
        {
            if (SetProperty(ref _selectedFlashMode, value))
            {
                OnPropertyChanged(nameof(IsSlotSelectionEnabled));
            }
        }
    }

    public FlashSlot SelectedFlashSlot
    {
        get => _selectedFlashSlot;
        set => SetProperty(ref _selectedFlashSlot, value);
    }

    public bool IsSlotSelectionEnabled => SelectedFlashMode is not FlashMode.Qspi;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string SelectedOperationText
    {
        get => _selectedOperationText;
        private set => SetProperty(ref _selectedOperationText, value);
    }

    public string UnhandledErrorMessage
    {
        get => _unhandledErrorMessage;
        private set => SetProperty(ref _unhandledErrorMessage, value);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(CanEditInputs));
                RefreshCommandStates();
            }
        }
    }

    public bool CanEditInputs => !IsRunning;

    public Task InitializeAsync(string[] args)
    {
        if (args.Length == 0)
        {
            return Task.CompletedTask;
        }

        var candidate = args[0];
        if (IsSupportedFile(candidate))
        {
            ApplySelectedFile(candidate);
            AddLog("INFO", "Startup", $"Loaded startup file argument '{candidate}'.");
        }
        else
        {
            AddLog("WARN", "Startup", $"Ignored unsupported startup argument '{candidate}'.");
        }

        return Task.CompletedTask;
    }

    public bool CanAcceptDrop(string[] filePaths)
    {
        return !IsRunning
               && filePaths.Length == 1
               && IsSupportedFile(filePaths[0]);
    }

    public void HandleDroppedFile(string filePath)
    {
        if (!IsSupportedFile(filePath))
        {
            StatusMessage = "Only .bin and .cap files can be dropped here.";
            AddLog("WARN", "UI", $"Rejected dropped file '{filePath}'.");
            return;
        }

        ApplySelectedFile(filePath);
        AddLog("INFO", "UI", $"Accepted dropped file '{filePath}'.");
    }

    public void ReportUnhandledError(string stage, Exception exception)
    {
        StatusMessage = "Failed";
        UnhandledErrorMessage = $"Unhandled {stage} error: {exception.Message}";
    }

    private bool CanStartOperation()
    {
        return !IsRunning && _validationResult.IsValid;
    }

    private void BrowseForFile()
    {
        var selectedPath = _fileDialogService.BrowseForFirmwareFile();
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        ApplySelectedFile(selectedPath);
    }

    private async Task SaveEnvironmentAsync()
    {
        var suggestedName = string.IsNullOrWhiteSpace(_currentProfileName)
            ? BuildDefaultProfileName()
            : _currentProfileName;

        var profileName = _userDialogService.PromptForProfileName(suggestedName);
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return;
        }

        var profile = new EnvironmentProfile
        {
            ProfileName = profileName.Trim(),
            HostIp = HostIp.Trim(),
            TargetIp = TargetIp.Trim(),
            HostUser = HostUser.Trim(),
            TargetUser = TargetUser.Trim(),
            LastFilePath = FilePath.Trim(),
            LastOperation = _lastSelectedOperation,
            LastFlashMode = SelectedFlashMode,
            LastFlashSlot = SelectedFlashSlot,
            UpdatedAt = DateTime.UtcNow
        };

        await _profileStore.SaveAsync(profile).ConfigureAwait(true);
        _currentProfileName = profile.ProfileName;

        AddLog("INFO", "Profiles", $"Saved environment profile '{profile.ProfileName}'.");
        _userDialogService.ShowInformation("Environment Saved", $"Saved environment profile '{profile.ProfileName}'.");
    }

    private async Task LoadEnvironmentAsync()
    {
        var profiles = await _profileStore.GetAllAsync().ConfigureAwait(true);
        if (profiles.Count == 0)
        {
            _userDialogService.ShowInformation("No Profiles", "No saved environment profiles were found.");
            return;
        }

        var selected = _userDialogService.PickProfile(profiles);
        if (selected is null)
        {
            return;
        }

        var loadedProfile = await _profileStore.LoadAsync(selected.ProfileName).ConfigureAwait(true) ?? selected;

        _currentProfileName = loadedProfile.ProfileName;
        _lastSelectedOperation = loadedProfile.LastOperation;
        HostIp = loadedProfile.HostIp;
        TargetIp = loadedProfile.TargetIp;
        HostUser = loadedProfile.HostUser;
        TargetUser = loadedProfile.TargetUser;
        FilePath = loadedProfile.LastFilePath;
        SelectedFlashMode = loadedProfile.LastFlashMode;
        SelectedFlashSlot = loadedProfile.LastFlashSlot;
        SelectedOperationText = GetOperationDisplayText(loadedProfile.LastOperation);

        AddLog("INFO", "Profiles", $"Loaded environment profile '{loadedProfile.ProfileName}'.");
    }

    private async Task ExecuteOperationAsync(OperationType operationType)
    {
        var context = BuildContext(operationType);
        _validationResult = _inputValidator.Validate(context);
        if (!_validationResult.IsValid)
        {
            UpdateValidationState();
            return;
        }

        if (!_userDialogService.ConfirmOperation(operationType, TargetIp.Trim(), FilePath.Trim()))
        {
            AddLog("INFO", "UI", "Operation start was cancelled at confirmation dialog.");
            return;
        }

        _lastSelectedOperation = operationType;
        SelectedOperationText = GetOperationDisplayText(operationType);
        UnhandledErrorMessage = string.Empty;
        IsRunning = true;
        StatusMessage = "Running";
        _currentOperationCts = new CancellationTokenSource();

        AddLog("INFO", "Validation", "------------------------------------------------------------");
        AddLog("INFO", "Validation", $"Starting {SelectedOperationText} workflow for '{FilePath.Trim()}'.");

        try
        {
            var progress = new Progress<LogEntry>(AppendLogEntry);
            var result = await _thorWorkflowOrchestrator
                .ExecuteAsync(context, progress, _currentOperationCts.Token)
                .ConfigureAwait(true);

            if (_currentOperationCts.IsCancellationRequested || result.SummaryMessage.Contains("cancel", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Cancelled";
            }
            else
            {
                StatusMessage = result.Success ? "Success" : "Failed";
            }

            AddLog(result.Success ? "INFO" : "ERROR", "Completion", result.SummaryMessage);
            if (result.Exception is not null)
            {
                AddLog("ERROR", "Completion", result.Exception.Message);
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled";
            AddLog("WARN", "Completion", "Operation cancelled.");
        }
        catch (Exception exception)
        {
            StatusMessage = "Failed";
            AddLog("ERROR", "Completion", exception.Message);
            AddLog("ERROR", "Completion", "Operation failed. Review the log output below for details.");
        }
        finally
        {
            _currentOperationCts?.Dispose();
            _currentOperationCts = null;
            IsRunning = false;
            RefreshCommandStates();
        }
    }

    private void Cancel()
    {
        if (!IsRunning)
        {
            return;
        }

        AddLog("WARN", "Completion", "Cancellation requested by user.");
        _currentOperationCts?.Cancel();
    }

    private void ApplySelectedFile(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    private OperationContext BuildContext(OperationType operationType)
    {
        var fullPath = string.IsNullOrWhiteSpace(FilePath) ? string.Empty : Path.GetFullPath(FilePath.Trim());

        return new OperationContext
        {
            HostIp = HostIp.Trim(),
            TargetIp = TargetIp.Trim(),
            HostUser = HostUser.Trim(),
            HostPassword = HostPassword.Trim(),
            TargetUser = TargetUser.Trim(),
            TargetPassword = TargetPassword.Trim(),
            FilePath = fullPath,
            OperationType = operationType,
            FlashMode = SelectedFlashMode,
            FlashSlot = SelectedFlashSlot,
            ScriptsRootPath = _thorScriptSettings.ScriptsRootPath,
            WorkingDirectory = DetermineScriptsWorkingDirectory()
        };
    }

    private void UpdateValidationState()
    {
        _validationResult = _inputValidator.Validate(HostIp.Trim(), TargetIp.Trim(), FilePath.Trim(), OperationType.Flash);

        if (IsRunning)
        {
            return;
        }

        UnhandledErrorMessage = string.Empty;
        StatusMessage = DetermineIdleStatus();
        RefreshCommandStates();
    }

    private string DetermineIdleStatus()
    {
        var hasAnyInput = !string.IsNullOrWhiteSpace(HostIp)
                          || !string.IsNullOrWhiteSpace(TargetIp)
                          || !string.IsNullOrWhiteSpace(FilePath);

        if (!hasAnyInput)
        {
            return "Idle";
        }

        return _validationResult.IsValid
            ? "Ready"
            : _validationResult.Errors.FirstOrDefault() ?? "Idle";
    }

    private void RefreshCommandStates()
    {
        BrowseCommand.RaiseCanExecuteChanged();
        SaveEnvironmentCommand.RaiseCanExecuteChanged();
        LoadEnvironmentCommand.RaiseCanExecuteChanged();
        FlashCommand.RaiseCanExecuteChanged();
        CapsuleUpdateCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        CopyLogCommand.RaiseCanExecuteChanged();
    }

    private bool IsSupportedFile(string? filePath)
    {
        return !string.IsNullOrWhiteSpace(filePath)
               && File.Exists(filePath)
               && _fileTypeResolver.IsSupportedPackage(filePath);
    }

    private string BuildDefaultProfileName()
    {
        if (!string.IsNullOrWhiteSpace(TargetIp))
        {
            return $"Target-{TargetIp.Trim()}";
        }

        return "Thor Environment";
    }

    private static string GetOperationDisplayText(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Flash => "Flash",
            OperationType.CapsuleUpdate => "Capsule Update",
            _ => "Not started"
        };
    }

    private string DetermineScriptsWorkingDirectory()
    {
        return Path.IsPathRooted(_thorScriptSettings.ScriptsRootPath)
            ? Path.GetFullPath(_thorScriptSettings.ScriptsRootPath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _thorScriptSettings.ScriptsRootPath));
    }

    private void AddLog(string level, string stage, string message)
    {
        AppendLogEntry(new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Stage = stage,
            Message = message
        });
    }

    private void AppendLogEntry(LogEntry entry)
    {
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(() => AppendLogEntry(entry));
            return;
        }

        Logs.Add(entry);

        if (string.IsNullOrEmpty(LogText))
        {
            LogText = entry.DisplayText;
            CopyLogCommand.RaiseCanExecuteChanged();
            return;
        }

        var builder = new StringBuilder(LogText.Length + entry.DisplayText.Length + Environment.NewLine.Length);
        builder.Append(LogText);
        builder.AppendLine();
        builder.Append(entry.DisplayText);
        LogText = builder.ToString();
        CopyLogCommand.RaiseCanExecuteChanged();
    }

    private void ClearLogs()
    {
        Logs.Clear();
        LogText = string.Empty;
        CopyLogCommand.RaiseCanExecuteChanged();
    }

    private void OnCommandException(Exception exception)
    {
        AddLog("ERROR", "Unhandled", exception.Message);
        StatusMessage = "Failed";
        UnhandledErrorMessage = exception.Message;
        IsRunning = false;
    }

    private bool CanCopyLog()
    {
        return !string.IsNullOrWhiteSpace(LogText);
    }

    private void CopyLog()
    {
        if (!CanCopyLog())
        {
            return;
        }

        _clipboardService.SetText(LogText);
        StatusMessage = "Log copied to clipboard";
    }
}
