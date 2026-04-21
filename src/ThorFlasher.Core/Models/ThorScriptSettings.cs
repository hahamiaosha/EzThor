namespace ThorFlasher.Core.Models;

public sealed class ThorScriptSettings
{
    public string ScriptsRootPath { get; set; } = "Thor_Script";

    public string SendBuildScriptRelativePath { get; set; } = "send_build.sh";

    public string FlashScriptRelativePath { get; set; } = "flash.sh";

    public string CapsuleScriptRelativePath { get; set; } = "Send_Cap_and_Update.sh";

    public string? IpConfigFileRelativePath { get; set; } = "env/target_config.sh";

    public string? HostIpToken { get; set; } = "{{HOST_IP}}";

    public string? TargetIpToken { get; set; } = "{{TARGET_IP}}";

    public string? SelectedFileToken { get; set; } = "{{SELECTED_FILE_PATH}}";

    public string? HostUserToken { get; set; } = "{{HOST_USER}}";

    public string? HostPasswordToken { get; set; } = "{{HOST_PASSWORD}}";

    public string? TargetUserToken { get; set; } = "{{TARGET_USER}}";

    public string? TargetPasswordToken { get; set; } = "{{TARGET_PASSWORD}}";
}
