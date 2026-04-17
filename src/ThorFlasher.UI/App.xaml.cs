using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThorFlasher.Adapters.ScriptExecution;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;
using ThorFlasher.Core.Services;
using ThorFlasher.Infrastructure.Persistence;
using ThorFlasher.Infrastructure.Runtime;
using ThorFlasher.UI.Services;
using ThorFlasher.UI.ViewModels;
using ThorFlasher.UI.Views;

namespace ThorFlasher.UI;

public partial class App : Application
{
    private IHost? _host;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host
            .CreateDefaultBuilder(e.Args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables(prefix: "ThorFlasher_");
            })
            .ConfigureServices((context, services) =>
            {
                var thorScriptSettings = context.Configuration
                    .GetSection("ThorScriptSettings")
                    .Get<ThorScriptSettings>() ?? new ThorScriptSettings();
                var scriptExecutionSettings = context.Configuration
                    .GetSection("ScriptExecutionSettings")
                    .Get<ScriptExecutionSettings>() ?? new ScriptExecutionSettings();

                services.AddSingleton(thorScriptSettings);
                services.AddSingleton(scriptExecutionSettings);

                services.AddSingleton<FileTypeResolver>();
                services.AddSingleton<InputValidator>();
                services.AddSingleton<ConnectivityValidator>();

                services.AddSingleton<IProcessRunner, ProcessRunner>();
                services.AddSingleton<IScriptConfigurationUpdater, ScriptConfigurationUpdater>();
                services.AddSingleton<IThorScriptResolver, ThorScriptResolver>();
                services.AddSingleton<IThorWorkflowOrchestrator, ThorWorkflowOrchestrator>();
                services.AddSingleton<IProfileStore, JsonProfileStore>();
                services.AddSingleton<ILogStore, JsonLogStore>();

                services.AddSingleton<IFileDialogService, FileDialogService>();
                services.AddSingleton<IClipboardService, ClipboardService>();
                services.AddSingleton<IUserDialogService, UserDialogService>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync().ConfigureAwait(true);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        await mainViewModel.InitializeAsync(e.Args).ConfigureAwait(true);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            _host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportUnhandledException("UI", e.Exception);
        e.Handled = true;
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ReportUnhandledException("Task", e.Exception);
        e.SetObserved();
    }

    private void ReportUnhandledException(string stage, Exception exception)
    {
        try
        {
            if (_host?.Services.GetService<MainViewModel>() is { } viewModel)
            {
                viewModel.ReportUnhandledError(stage, exception);
                return;
            }
        }
        catch
        {
            // Fall back to message box if the main view model is unavailable.
        }

        MessageBox.Show(
            exception.Message,
            $"Unhandled {stage} Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
