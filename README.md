# ThorFlasher v2

ThorFlasher v2 is a Windows WPF `.NET 8` desktop application that acts as a GUI wrapper around existing `Thor_Script` shell scripts. It does not implement a new THOR flashing backend. The app validates input, updates script-side configuration tokens, runs `send_build.sh`, then runs either the configured Flash script or Capsule Update script.

## Flow

The app executes this sequence:

1. Validate THOR Host IP, THOR Target IP, selected file, and chosen operation
2. Update the configured script-side token file with:
   - host IP
   - target IP
   - selected package path
3. Run `send_build.sh`
4. Run either:
   - Flash script
   - Capsule Update script
5. Stream live logs and report success, failure, or cancellation

For the current `Flash` flow, the target must already be placed into recovery mode manually before you click `Flash`. ThorFlasher no longer sends `reboot --force forced-recovery` to the target.

The clicked button determines the operation. File extension no longer selects the workflow.

## What it supports

- `.bin` and `.cap` package selection
- drag and drop into the app
- startup file argument when a file is dragged onto `ThorFlasher.exe`
- explicit `Flash` and `Capsule Update` buttons
- save/load environment profiles
- live log streaming
- cancel while a script is running

## Build

This repo includes a local .NET SDK under `.dotnet`, so you can build with either your system SDK or the bundled one.

Using the bundled SDK:

```powershell
.\.dotnet\dotnet.exe restore ThorFlasher.sln
.\.dotnet\dotnet.exe build ThorFlasher.sln
```

Using a system-installed SDK:

```powershell
dotnet restore ThorFlasher.sln
dotnet build ThorFlasher.sln
```

## Run

```powershell
.\.dotnet\dotnet.exe run --project .\src\ThorFlasher.UI\ThorFlasher.UI.csproj
```

With a startup file:

```powershell
.\.dotnet\dotnet.exe run --project .\src\ThorFlasher.UI\ThorFlasher.UI.csproj -- "C:\path\to\firmware.bin"
```

## Publish

```powershell
.\.dotnet\dotnet.exe publish .\src\ThorFlasher.UI\ThorFlasher.UI.csproj -c Release -r win-x64 --self-contained false
```

The executable name is exactly `ThorFlasher.exe`.

Typical output paths:

- Debug build: `src\ThorFlasher.UI\bin\Debug\net8.0-windows\ThorFlasher.exe`
- Release build: `src\ThorFlasher.UI\bin\Release\net8.0-windows\ThorFlasher.exe`
- Published app: `src\ThorFlasher.UI\bin\Release\net8.0-windows\win-x64\publish\ThorFlasher.exe`

You can also use:

```powershell
.\Launch-ThorFlasher.cmd
```

## Configuration

The main configuration file is:

- `src/ThorFlasher.UI/appsettings.json`

### ThorScriptSettings

These values point ThorFlasher to your `Thor_Script` assets:

- `ScriptsRootPath`
- `SendBuildScriptRelativePath`
- `FlashScriptRelativePath`
- `CapsuleScriptRelativePath`
- `IpConfigFileRelativePath`
- `HostIpToken`
- `TargetIpToken`
- `SelectedFileToken`

Default placeholders are intentionally obvious. Adjust them to match the real `Thor_Script` branch contents.

Important:

- `FlashScriptRelativePath` defaults to `flash.sh` as a placeholder.
- The observed WSL script set included `send_build.sh` and `Send_Cap_and_Update.sh`.
- No obvious flash script was found in that observed location, so you must update `FlashScriptRelativePath` to the real script name when known.

### ScriptExecutionSettings

These values control how `.sh` files are launched from Windows:

- `ShellExecutable`
- `ShellArgumentsTemplate`
- `SendBuildArgumentsTemplate`
- `FlashArgumentsTemplate`
- `CapsuleArgumentsTemplate`
- `TranslateWindowsPathsForWsl`

Default example:

```json
"ScriptExecutionSettings": {
  "ShellExecutable": "bash.exe",
  "ShellArgumentsTemplate": "{scriptPath} {args}",
  "SendBuildArgumentsTemplate": "",
  "FlashArgumentsTemplate": "",
  "CapsuleArgumentsTemplate": "",
  "TranslateWindowsPathsForWsl": false
}
```

## WSL example

If your scripts live under WSL instead of a copied local `Thor_Script` folder, configure an absolute Windows-accessible path and switch to `wsl.exe` if needed.

Example shape:

```json
"ThorScriptSettings": {
  "ScriptsRootPath": "\\\\wsl.localhost\\Ubuntu-22.04\\build\\QNAP\\qai-th1250",
  "SendBuildScriptRelativePath": "send_build.sh",
  "FlashScriptRelativePath": "flash.sh",
  "CapsuleScriptRelativePath": "Send_Cap_and_Update.sh",
  "IpConfigFileRelativePath": "env/target_config.sh",
  "HostIpToken": "{{HOST_IP}}",
  "TargetIpToken": "{{TARGET_IP}}",
  "SelectedFileToken": "{{SELECTED_FILE_PATH}}"
},
"ScriptExecutionSettings": {
  "ShellExecutable": "wsl.exe",
  "ShellArgumentsTemplate": "{scriptPath} {args}",
  "SendBuildArgumentsTemplate": "",
  "FlashArgumentsTemplate": "",
  "CapsuleArgumentsTemplate": "",
  "TranslateWindowsPathsForWsl": true
}
```

You may need to adjust `ShellArgumentsTemplate` for your exact shell environment.

## Script-side token file

ThorFlasher updates exactly one configured file before execution. That file should contain tokens like:

```bash
REMOTE_HOST="{{HOST_IP}}"
TARGET_HOST="{{TARGET_IP}}"
SRC="{{SELECTED_FILE_PATH}}"
```

ThorFlasher will:

- create a sibling `.bak` backup
- replace only the configured tokens
- fail fast if expected tokens are missing

If your current scripts hardcode values directly, create a dedicated config/template file consumed by those scripts and point `IpConfigFileRelativePath` at it.

## Profiles and logs

- Profiles: `%AppData%\ThorFlasher\Profiles\`
- Logs: `%AppData%\ThorFlasher\Logs\`

The UI currently supports profile save/load. Log persistence infrastructure is implemented for future UI exposure.

## Testing

```powershell
.\.dotnet\dotnet.exe test .\tests\ThorFlasher.Core.Tests\ThorFlasher.Core.Tests.csproj
```

## Important assumptions

- ThorFlasher v2 is a GUI wrapper around existing `Thor_Script` scripts.
- Both Flash and Capsule Update run `send_build.sh` first.
- The clicked button decides which follow-up script runs.
- IP values entered in the UI are applied to the configured script-side token file before execution.
- Exact script names, script arguments, and shell host details may need adjustment for the real `Thor_Script` branch you use.
