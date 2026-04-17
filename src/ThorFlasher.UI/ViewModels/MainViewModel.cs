using System.Collections.ObjectModel;
using System.IO;
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
    private readonly OperationCoordinator _operationCoordinator;
    private readonly ConnectivityValidator _connectivityValidator;
    private readonly IProfileStore _profileStore;
    private readonly IFileDialogService _fileDialogService;
    private readonly IUserDialogService _userDialogService;

    private string _hostIp = string.Empty;
    private string _targetIp = string.Empty;
    private string _filePath = string.Empty;
    private string _status = "Idle";
    private string _currentProfileName = string.Empty;
    private bool _isRunning;
    private ValidationResult _validationResult = ValidationResult.Failure("Enter THOR Host IP, THOR Target IP, and a valid file.");
    private CancellationTokenSource? _currentOperationCts;
    private OperationType _detectedOperation;

    public MainViewModel(
        FileTypeResolver fileTypeResolver,
        InputValidator inputValidator,
        OperationCoordinator operationCoordinator,
        ConnectivityValidator connectivityValidator,
        IProfileStore profileStore,
        IFileDialogService fileDialogService,
        IUserDialogService userDialogService)
    {
        _fileTypeResolver = fileTypeResolver;
        _inputValidator = inputValidator;
        _operationCoordinator = operationCoordinator;
        _connectivityValidator = connectivityValidator;
        _profileStore = profileStore;
        _fileDialogService = fileDialogService;
        _userDialogService = userDialogService;

        LogEntries = new ObservableCollection<LogEntry>();

        BrowseCommand = new RelayCommand(BrowseForFile, () => !IsRunning);
        SaveEnvironmentCommand = new AsyncRelayCommand(SaveEnvironmentAsync, () => !IsRunning);
        LoadEnvironmentCommand = new AsyncRelayCommand(LoadEnvironmentAsync, () => !IsRunning);
        StartCommand = new AsyncRelayCommand(StartAsync, CanStart);
        CancelCommand = new RelayCommand(Cancel, () => IsRunning);
        ClearLogCommand = new RelayCommand(() => LogEntries.Clear());

        UpdateStateAfterInputChange();
    }

    public ObservableCollection<LogEntry> LogEntries { get; }

    public RelayCommand BrowseCommand { get; }

    public AsyncRelayCommand SaveEnvironmentCommand { get; }

    public AsyncRelayCommand LoadEnvironmentCommand { get; }

    public AsyncRelayCommand StartCommand { get; }

    public RelayCommand CancelCommand { get; }

    public RelayCommand ClearLogCommand { get; }

    public string HostIp
    {
        get => _hostIp;
        set
        {
            if (SetProperty(ref _hostIp, value))
            {
                UpdateStateAfterInputChange();
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
                UpdateStateAfterInputChange();
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
                UpdateStateAfterInputChange();
            }
        }
    }

    public OperationType DetectedOperation
    {
        get => _detectedOperation;
        private set
        {
            if (SetProperty(ref _detectedOperation, value))
            {
                OnPropertyChanged(nameof(DetectedOperationDisplay));
            }
        }
    }

    public string DetectedOperationDisplay => DetectedOperation switch
    {
        OperationType.BinFlash => "BIN Flash",
        OperationType.CapUpdate => "CAP Update",
        _ => "Unknown"
    };

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
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
            Status = "Only .bin and .cap files can be dropped here.";
            AddLog("WARN", "UI", $"Rejected dropped file '{filePath}'.");
            return;
        }

        ApplySelectedFile(filePath);
        AddLog("INFO", "UI", $"Accepted dropped file '{filePath}'.");
    }

    private bool CanStart()
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
            LastFilePath = FilePath.Trim(),
            LastOperation = DetectedOperation,
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
        HostIp = loadedProfile.HostIp;
        TargetIp = loadedProfile.TargetIp;
        FilePath = loadedProfile.LastFilePath;

        AddLog("INFO", "Profiles", $"Loaded environment profile '{loadedProfile.ProfileName}'.");
    }

    private async Task StartAsync()
    {
        var context = BuildContext();
        _validationResult = _inputValidator.Validate(context);
        if (!_validationResult.IsValid)
        {
            UpdateStateAfterInputChange();
            return;
        }

        if (!_userDialogService.ConfirmOperation(DetectedOperation, TargetIp.Trim(), FilePath.Trim()))
        {
            AddLog("INFO", "UI", "Operation start was cancelled at confirmation dialog.");
            return;
        }

        IsRunning = true;
        Status = "Running";
        _currentOperationCts = new CancellationTokenSource();

        AddLog("INFO", "Operation", "------------------------------------------------------------");
        AddLog("INFO", "Operation", $"Starting {DetectedOperationDisplay} for {FilePath.Trim()}.");

        try
        {
            var progress = new Progress<LogEntry>(entry => LogEntries.Add(entry));

            var connectivityOkay = await _connectivityValidator
                .ValidateAsync(context, progress, _currentOperationCts.Token)
                .ConfigureAwait(true);

            if (!connectivityOkay)
            {
                Status = "Failed";
                AddLog("ERROR", "Connectivity", "Connectivity validation failed.");
                return;
            }

            var result = await _operationCoordinator
                .ExecuteAsync(context, progress, _currentOperationCts.Token)
                .ConfigureAwait(true);

            if (_currentOperationCts.IsCancellationRequested || result.SummaryMessage.Contains("cancel", StringComparison.OrdinalIgnoreCase))
            {
                Status = "Cancelled";
            }
            else
            {
                Status = result.Success ? "Success" : "Failed";
            }

            AddLog(result.Success ? "INFO" : "ERROR", "Operation", result.SummaryMessage);
            if (result.Exception is not null)
            {
                AddLog("ERROR", "Operation", result.Exception.Message);
            }
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
            AddLog("WARN", "Operation", "Operation cancelled.");
        }
        catch (Exception exception)
        {
            Status = "Failed";
            AddLog("ERROR", "Operation", exception.Message);
            _userDialogService.ShowError("Operation Failed", exception.Message);
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

        AddLog("WARN", "Operation", "Cancellation requested by user.");
        _currentOperationCts?.Cancel();
    }

    private void ApplySelectedFile(string filePath)
    {
        FilePath = Path.GetFullPath(filePath);
    }

    private OperationContext BuildContext()
    {
        var fullPath = string.IsNullOrWhiteSpace(FilePath) ? string.Empty : Path.GetFullPath(FilePath.Trim());

        return new OperationContext
        {
            HostIp = HostIp.Trim(),
            TargetIp = TargetIp.Trim(),
            FilePath = fullPath,
            WorkingDirectory = string.IsNullOrWhiteSpace(fullPath)
                ? Environment.CurrentDirectory
                : Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory
        };
    }

    private void UpdateStateAfterInputChange()
    {
        DetectedOperation = _fileTypeResolver.Resolve(FilePath);
        _validationResult = _inputValidator.Validate(HostIp.Trim(), TargetIp.Trim(), FilePath.Trim());

        if (IsRunning)
        {
            return;
        }

        Status = DetermineIdleStatus();
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
        StartCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
    }

    private bool IsSupportedFile(string? filePath)
    {
        return !string.IsNullOrWhiteSpace(filePath)
               && File.Exists(filePath)
               && _fileTypeResolver.Resolve(filePath) != OperationType.Unknown;
    }

    private string BuildDefaultProfileName()
    {
        if (!string.IsNullOrWhiteSpace(TargetIp))
        {
            return $"Target-{TargetIp.Trim()}";
        }

        return "Thor Environment";
    }

    private void AddLog(string level, string stage, string message)
    {
        LogEntries.Add(new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Stage = stage,
            Message = message
        });
    }
}
