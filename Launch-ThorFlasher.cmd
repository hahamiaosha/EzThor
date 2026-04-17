@echo off
setlocal

set "ROOT=%~dp0"
set "PROJECT=%ROOT%src\ThorFlasher.UI\ThorFlasher.UI.csproj"
set "PUBLISH_EXE=%ROOT%src\ThorFlasher.UI\bin\Release\net8.0-windows\win-x64\publish\ThorFlasher.exe"
set "RELEASE_EXE=%ROOT%src\ThorFlasher.UI\bin\Release\net8.0-windows\ThorFlasher.exe"
set "DEBUG_EXE=%ROOT%src\ThorFlasher.UI\bin\Debug\net8.0-windows\ThorFlasher.exe"

if exist "%PUBLISH_EXE%" goto launch_publish
if exist "%RELEASE_EXE%" goto launch_release
if exist "%DEBUG_EXE%" goto launch_debug

where dotnet >nul 2>&1
if errorlevel 1 goto missing_dotnet

echo No built ThorFlasher.exe was found.
echo Building the app now...
dotnet build "%PROJECT%" -c Debug
if errorlevel 1 goto build_failed

if exist "%DEBUG_EXE%" goto launch_debug

echo Build finished, but ThorFlasher.exe was not found.
goto end

:launch_publish
echo Launching published ThorFlasher.exe...
start "" "%PUBLISH_EXE%" %*
goto end

:launch_release
echo Launching release ThorFlasher.exe...
start "" "%RELEASE_EXE%" %*
goto end

:launch_debug
echo Launching debug ThorFlasher.exe...
start "" "%DEBUG_EXE%" %*
goto end

:missing_dotnet
echo dotnet was not found on PATH.
echo Install the .NET 8 SDK and try again.
echo https://dotnet.microsoft.com/download/dotnet/8.0
goto end

:build_failed
echo Build failed. Review the output above for details.

:end
endlocal
