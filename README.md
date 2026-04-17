# ThorFlasher

ThorFlasher is a Windows WPF desktop application for preparing and running THOR firmware operations against a target device. It supports `.bin` flash flows, `.cap` capsule update flows, drag and drop, startup file arguments, environment profile save/load, live log streaming, cancellation, and placeholder command execution that can be replaced with real THOR commands later.

## What the app does

- Accepts a firmware or capsule file via browse, drag and drop, or by dragging the file onto the built `.exe`
- Collects THOR host and target IP addresses
- Auto-detects the operation type from the selected file extension
- Saves and loads local environment profiles as JSON
- Runs the configured external command for the detected operation and streams logs live
- Keeps the UI independent from the concrete flashing command implementation

## Build

Install the .NET 8 SDK, then run:

```powershell
dotnet restore
dotnet build ThorFlasher.sln
```

## Run

```powershell
dotnet run --project .\src\ThorFlasher.UI\ThorFlasher.UI.csproj
```

You can also preload a file:

```powershell
dotnet run --project .\src\ThorFlasher.UI\ThorFlasher.UI.csproj -- "C:\path\to\firmware.bin"
```

After building, the executable name is exactly `ThorFlasher.exe`.

Typical output paths:

- Debug build: `src\ThorFlasher.UI\bin\Debug\net8.0-windows\ThorFlasher.exe`
- Release build: `src\ThorFlasher.UI\bin\Release\net8.0-windows\ThorFlasher.exe`
- Published app: `src\ThorFlasher.UI\bin\Release\net8.0-windows\win-x64\publish\ThorFlasher.exe`

You can also use the repo-root launcher script:

```powershell
.\Launch-ThorFlasher.cmd
```

To preload a file through the launcher script:

```powershell
.\Launch-ThorFlasher.cmd "C:\path\to\firmware.bin"
```

After publishing, you can drag a `.bin` or `.cap` file directly onto `ThorFlasher.exe` in Windows Explorer.

## Publish a Windows executable

```powershell
dotnet publish .\src\ThorFlasher.UI\ThorFlasher.UI.csproj -c Release -r win-x64 --self-contained false
```

## Profile and log storage

- Profiles: `%AppData%\ThorFlasher\Profiles\`
- Logs: `%AppData%\ThorFlasher\Logs\`

## Replacing the placeholder THOR commands

The placeholder command templates live in `src/ThorFlasher.UI/appsettings.json`.

- Replace this placeholder with the real THOR BIN flash command
- Replace this placeholder with the real THOR CAP update command

The adapters expand these placeholders:

- `{hostIp}`
- `{targetIp}`
- `{filePath}`

Important:

- Keep `{filePath}` as a bare placeholder in the template. The application escapes placeholder values centrally before launching the process.
- The UI does not need to change when you replace the placeholder command definitions with real THOR commands.

## Solution structure

- `src/ThorFlasher.UI`: WPF app, MVVM, dialogs, dependency injection, config loading
- `src/ThorFlasher.Core`: domain models, validation, coordination contracts
- `src/ThorFlasher.Adapters`: `.bin` and `.cap` operation adapters
- `src/ThorFlasher.Infrastructure`: JSON persistence and process execution
- `tests/ThorFlasher.Core.Tests`: unit tests for core behavior

## Notes

- This repo was generated to stay runnable even before the proprietary THOR command sequence is known.
- The shell in this environment did not expose `dotnet`, so local compile verification depends on installing or adding the .NET 8 SDK to `PATH`.
