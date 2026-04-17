using ThorFlasher.Adapters.ScriptExecution;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Tests;

public sealed class ThorScriptResolverTests
{
    [Fact]
    public void GetSendBuildCommand_ResolvesShellCommand()
    {
        var scriptsRoot = CreateScriptsRoot();
        var resolver = CreateResolver(scriptsRoot);
        var context = CreateContext(OperationType.Flash, scriptsRoot);

        var command = resolver.GetSendBuildCommand(context);

        Assert.Equal("bash.exe", command.Executable);
        Assert.Contains("send_build.sh", command.ArgumentsTemplate, StringComparison.OrdinalIgnoreCase);
        Assert.True(command.Enabled);
    }

    [Fact]
    public void GetSendBuildCommand_UsesBashFriendlyScriptPath_ForBashExe()
    {
        var scriptsRoot = CreateScriptsRoot();
        var resolver = CreateResolver(scriptsRoot);
        var context = CreateContext(OperationType.Flash, scriptsRoot);

        var command = resolver.GetSendBuildCommand(context);

        Assert.Contains("./send_build.sh", command.ArgumentsTemplate, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"C:\", command.ArgumentsTemplate, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OperationType.Flash, "flash.sh")]
    [InlineData(OperationType.CapsuleUpdate, "Send_Cap_and_Update.sh")]
    public void GetOperationCommand_SelectsScriptBasedOnOperation(OperationType operationType, string expectedScriptName)
    {
        var scriptsRoot = CreateScriptsRoot();
        var resolver = CreateResolver(scriptsRoot);
        var context = CreateContext(operationType, scriptsRoot);

        var command = resolver.GetOperationCommand(context);

        Assert.Contains($"./{expectedScriptName}", command.ArgumentsTemplate, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetSendBuildCommand_ExpandsWslFriendlyPaths_WhenConfigured()
    {
        var scriptsRoot = CreateScriptsRoot();
        var resolver = new ThorScriptResolver(
            new ThorScriptSettings
            {
                ScriptsRootPath = scriptsRoot,
                SendBuildScriptRelativePath = "send_build.sh"
            },
            new ScriptExecutionSettings
            {
                ShellExecutable = "wsl.exe",
                ShellArgumentsTemplate = "{scriptPath} {args}",
                SendBuildArgumentsTemplate = "{filePath}",
                TranslateWindowsPathsForWsl = true
            });

        var command = resolver.GetSendBuildCommand(new OperationContext
        {
            HostIp = "10.0.0.1",
            TargetIp = "10.0.0.2",
            FilePath = @"C:\images\uefi.bin",
            ScriptsRootPath = scriptsRoot,
            WorkingDirectory = scriptsRoot,
            OperationType = OperationType.Flash
        });

        Assert.Contains("/mnt/", command.ArgumentsTemplate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uefi.bin", command.ArgumentsTemplate, StringComparison.OrdinalIgnoreCase);
    }

    private static ThorScriptResolver CreateResolver(string scriptsRoot)
    {
        return new ThorScriptResolver(
            new ThorScriptSettings
            {
                ScriptsRootPath = scriptsRoot,
                SendBuildScriptRelativePath = "send_build.sh",
                FlashScriptRelativePath = "flash.sh",
                CapsuleScriptRelativePath = "Send_Cap_and_Update.sh"
            },
            new ScriptExecutionSettings
            {
                ShellExecutable = "bash.exe",
                ShellArgumentsTemplate = "{scriptPath} {args}",
                SendBuildArgumentsTemplate = string.Empty,
                FlashArgumentsTemplate = string.Empty,
                CapsuleArgumentsTemplate = string.Empty
            });
    }

    private static OperationContext CreateContext(OperationType operationType, string scriptsRoot)
    {
        return new OperationContext
        {
            HostIp = "10.0.0.1",
            TargetIp = "10.0.0.2",
            FilePath = @"C:\images\firmware.bin",
            OperationType = operationType,
            ScriptsRootPath = scriptsRoot,
            WorkingDirectory = scriptsRoot
        };
    }

    private static string CreateScriptsRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Thor_Script");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "send_build.sh"), "#!/bin/bash");
        File.WriteAllText(Path.Combine(root, "flash.sh"), "#!/bin/bash");
        File.WriteAllText(Path.Combine(root, "Send_Cap_and_Update.sh"), "#!/bin/bash");
        return root;
    }
}
