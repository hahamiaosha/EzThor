using ThorFlasher.Core.Models;
using ThorFlasher.Infrastructure.Runtime;

namespace ThorFlasher.Core.Tests;

public sealed class ScriptConfigurationUpdaterTests
{
    [Fact]
    public async Task ApplyIpSettingsAsync_ReplacesTokensAndCreatesBackup()
    {
        var scriptsRoot = CreateTempDirectory();
        var envDirectory = Path.Combine(scriptsRoot, "env");
        Directory.CreateDirectory(envDirectory);

        var configPath = Path.Combine(envDirectory, "target_config.sh");
        await File.WriteAllTextAsync(configPath, "HOST={{HOST_IP}}\nTARGET={{TARGET_IP}}\nFILE={{SELECTED_FILE_PATH}}\nREMOTE_USER={{HOST_USER}}\nREMOTE_PASS={{HOST_PASSWORD}}\nTARGET_USER={{TARGET_USER}}\nTARGET_PASS={{TARGET_PASSWORD}}\n");

        var updater = new ScriptConfigurationUpdater(new ThorScriptSettings
        {
            ScriptsRootPath = scriptsRoot,
            IpConfigFileRelativePath = @"env\target_config.sh",
            HostIpToken = "{{HOST_IP}}",
            TargetIpToken = "{{TARGET_IP}}",
            SelectedFileToken = "{{SELECTED_FILE_PATH}}"
        });

        var context = new OperationContext
        {
            HostIp = "10.0.0.1",
            TargetIp = "10.0.0.2",
            HostUser = "admin",
            HostPassword = "secret",
            TargetUser = "qnap",
            TargetPassword = "1111",
            FilePath = @"C:\images\fw.bin",
            ScriptsRootPath = scriptsRoot,
            WorkingDirectory = scriptsRoot,
            OperationType = OperationType.Flash
        };

        await updater.ApplyIpSettingsAsync(context, new Progress<LogEntry>(), CancellationToken.None);

        var updated = await File.ReadAllTextAsync(configPath);
        Assert.Contains("10.0.0.1", updated);
        Assert.Contains("10.0.0.2", updated);
        Assert.Contains(@"C:\images\fw.bin", updated);
        Assert.True(File.Exists($"{configPath}.bak"));
    }

    [Fact]
    public async Task ApplyIpSettingsAsync_Throws_WhenConfiguredTokenIsMissing()
    {
        var scriptsRoot = CreateTempDirectory();
        var envDirectory = Path.Combine(scriptsRoot, "env");
        Directory.CreateDirectory(envDirectory);

        var configPath = Path.Combine(envDirectory, "target_config.sh");
        await File.WriteAllTextAsync(configPath, "HOST=STATIC\n");

        var updater = new ScriptConfigurationUpdater(new ThorScriptSettings
        {
            ScriptsRootPath = scriptsRoot,
            IpConfigFileRelativePath = @"env\target_config.sh",
            HostIpToken = "{{HOST_IP}}",
            TargetIpToken = "{{TARGET_IP}}"
        });

        var context = new OperationContext
        {
            HostIp = "10.0.0.1",
            TargetIp = "10.0.0.2",
            HostUser = "admin",
            HostPassword = "secret",
            TargetUser = "qnap",
            TargetPassword = "1111",
            FilePath = @"C:\images\fw.bin",
            ScriptsRootPath = scriptsRoot,
            WorkingDirectory = scriptsRoot,
            OperationType = OperationType.Flash
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            updater.ApplyIpSettingsAsync(context, new Progress<LogEntry>(), CancellationToken.None));
    }

    [Fact]
    public async Task ApplyIpSettingsAsync_UpdatesExistingAssignments_WhenTokensWereAlreadyReplaced()
    {
        var scriptsRoot = CreateTempDirectory();
        var envDirectory = Path.Combine(scriptsRoot, "env");
        Directory.CreateDirectory(envDirectory);

        var configPath = Path.Combine(envDirectory, "target_config.sh");
        await File.WriteAllTextAsync(
            configPath,
            """
            THOR_HOST_IP="10.0.0.10"
            THOR_TARGET_IP="10.0.0.20"
            SELECTED_FILE_PATH="C:\\old\\fw.bin"
            REMOTE_USER="olduser"
            REMOTE_PASS="oldpass"
            TARGET_USER="oldtarget"
            TARGET_PASS="oldtpass"
            """);

        var updater = new ScriptConfigurationUpdater(new ThorScriptSettings
        {
            ScriptsRootPath = scriptsRoot,
            IpConfigFileRelativePath = @"env\target_config.sh",
            HostIpToken = "{{HOST_IP}}",
            TargetIpToken = "{{TARGET_IP}}",
            SelectedFileToken = "{{SELECTED_FILE_PATH}}"
        });

        var context = new OperationContext
        {
            HostIp = "10.0.0.1",
            TargetIp = "10.0.0.2",
            HostUser = "admin",
            HostPassword = "secret",
            TargetUser = "qnap",
            TargetPassword = "1111",
            FilePath = @"C:\images\fw.bin",
            ScriptsRootPath = scriptsRoot,
            WorkingDirectory = scriptsRoot,
            OperationType = OperationType.Flash
        };

        await updater.ApplyIpSettingsAsync(context, new Progress<LogEntry>(), CancellationToken.None);

        var updated = await File.ReadAllTextAsync(configPath);
        Assert.Contains("THOR_HOST_IP=\"10.0.0.1\"", updated);
        Assert.Contains("THOR_TARGET_IP=\"10.0.0.2\"", updated);
        Assert.Contains("SELECTED_FILE_PATH=\"C:\\\\images\\\\fw.bin\"", updated);
        Assert.Contains("REMOTE_USER=\"admin\"", updated);
        Assert.Contains("REMOTE_PASS=\"secret\"", updated);
        Assert.Contains("TARGET_USER=\"qnap\"", updated);
        Assert.Contains("TARGET_PASS=\"1111\"", updated);
        Assert.True(File.Exists($"{configPath}.bak"));
    }

    [Fact]
    public async Task ApplyIpSettingsAsync_KeepsExistingOptionalValues_WhenOverridesAreBlank()
    {
        var scriptsRoot = CreateTempDirectory();
        var envDirectory = Path.Combine(scriptsRoot, "env");
        Directory.CreateDirectory(envDirectory);

        var configPath = Path.Combine(envDirectory, "target_config.sh");
        await File.WriteAllTextAsync(
            configPath,
            """
            THOR_HOST_IP="10.0.0.10"
            THOR_TARGET_IP="10.0.0.20"
            SELECTED_FILE_PATH="C:\\old\\fw.bin"
            REMOTE_USER="olduser"
            REMOTE_PASS="oldpass"
            TARGET_USER="oldtarget"
            TARGET_PASS="oldtpass"
            """);

        var updater = new ScriptConfigurationUpdater(new ThorScriptSettings
        {
            ScriptsRootPath = scriptsRoot,
            IpConfigFileRelativePath = @"env\target_config.sh",
            HostIpToken = "{{HOST_IP}}",
            TargetIpToken = "{{TARGET_IP}}",
            SelectedFileToken = "{{SELECTED_FILE_PATH}}",
            HostUserToken = "{{HOST_USER}}",
            HostPasswordToken = "{{HOST_PASSWORD}}",
            TargetUserToken = "{{TARGET_USER}}",
            TargetPasswordToken = "{{TARGET_PASSWORD}}"
        });

        var context = new OperationContext
        {
            HostIp = "10.0.0.1",
            TargetIp = "10.0.0.2",
            HostUser = string.Empty,
            HostPassword = string.Empty,
            TargetUser = string.Empty,
            TargetPassword = string.Empty,
            FilePath = string.Empty,
            ScriptsRootPath = scriptsRoot,
            WorkingDirectory = scriptsRoot,
            OperationType = OperationType.Flash
        };

        await updater.ApplyIpSettingsAsync(context, new Progress<LogEntry>(), CancellationToken.None);

        var updated = await File.ReadAllTextAsync(configPath);
        Assert.Contains("THOR_HOST_IP=\"10.0.0.1\"", updated);
        Assert.Contains("THOR_TARGET_IP=\"10.0.0.2\"", updated);
        Assert.Contains("SELECTED_FILE_PATH=\"C:\\old\\fw.bin\"", updated);
        Assert.Contains("REMOTE_USER=\"olduser\"", updated);
        Assert.Contains("REMOTE_PASS=\"oldpass\"", updated);
        Assert.Contains("TARGET_USER=\"oldtarget\"", updated);
        Assert.Contains("TARGET_PASS=\"oldtpass\"", updated);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
