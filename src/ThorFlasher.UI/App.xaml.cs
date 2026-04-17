using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThorFlasher.Adapters.Adapters;
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
                var commandOptions = context.Configuration
                    .GetSection("ThorCommandOptions")
                    .Get<ThorCommandOptions>() ?? new ThorCommandOptions();

                services.AddSingleton(commandOptions);

                services.AddSingleton<FileTypeResolver>();
                services.AddSingleton<InputValidator>();
                services.AddSingleton<OperationCoordinator>();
                services.AddSingleton<ConnectivityValidator>();

                services.AddSingleton<IProcessRunner, ProcessRunner>();
                services.AddSingleton<IProfileStore, JsonProfileStore>();
                services.AddSingleton<ILogStore, JsonLogStore>();
                services.AddSingleton<IThorOperationAdapter, BinFlashAdapter>();
                services.AddSingleton<IThorOperationAdapter, CapUpdateAdapter>();

                services.AddSingleton<IFileDialogService, FileDialogService>();
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
}
