using System.ComponentModel;
using System.Diagnostics;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Infrastructure.Runtime;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<OperationResult> RunAsync(ProcessRunRequest request, IProgress<LogEntry> progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(progress);

        if (string.IsNullOrWhiteSpace(request.Executable))
        {
            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = "Executable is not configured."
            };
        }

        var workingDirectory = ResolveWorkingDirectory(request.WorkingDirectory);
        Directory.CreateDirectory(workingDirectory);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = request.Executable,
                Arguments = request.Arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var outputCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = false;

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is null)
            {
                outputCompletion.TrySetResult(true);
                return;
            }

            progress.Report(CreateLog("INFO", request.Stage, eventArgs.Data));
        };

        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is null)
            {
                errorCompletion.TrySetResult(true);
                return;
            }

            progress.Report(CreateLog("ERROR", request.Stage, eventArgs.Data));
        };

        try
        {
            progress.Report(CreateLog("INFO", request.Stage, $"Launching '{request.Executable}' in '{workingDirectory}'."));

            if (!process.Start())
            {
                return new OperationResult
                {
                    Success = false,
                    ExitCode = -1,
                    SummaryMessage = "The configured process failed to start."
                };
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var registration = token.Register(() =>
            {
                cancellationObserved = true;

                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Best effort only. The result object below will still surface cancellation.
                }
            });

            await process.WaitForExitAsync().ConfigureAwait(false);
            await Task.WhenAll(outputCompletion.Task, errorCompletion.Task).ConfigureAwait(false);

            if (cancellationObserved || token.IsCancellationRequested)
            {
                progress.Report(CreateLog("WARN", request.Stage, "Operation was cancelled."));
                return new OperationResult
                {
                    Success = false,
                    ExitCode = -1,
                    SummaryMessage = "Operation cancelled."
                };
            }

            var succeeded = process.ExitCode == 0;
            progress.Report(CreateLog(succeeded ? "INFO" : "ERROR", request.Stage, $"Process exited with code {process.ExitCode}."));

            return new OperationResult
            {
                Success = succeeded,
                ExitCode = process.ExitCode,
                SummaryMessage = succeeded
                    ? "Operation completed successfully."
                    : $"Operation failed with exit code {process.ExitCode}."
            };
        }
        catch (Win32Exception exception)
        {
            progress.Report(CreateLog("ERROR", request.Stage, exception.Message));
            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = "Unable to start the configured process.",
                Exception = exception
            };
        }
        catch (Exception exception)
        {
            progress.Report(CreateLog("ERROR", request.Stage, exception.Message));
            return new OperationResult
            {
                Success = false,
                ExitCode = -1,
                SummaryMessage = "Unexpected error while running the configured process.",
                Exception = exception
            };
        }
    }

    private static string ResolveWorkingDirectory(string? workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            return workingDirectory;
        }

        return Environment.CurrentDirectory;
    }

    private static LogEntry CreateLog(string level, string stage, string message)
    {
        return new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Stage = stage,
            Message = message
        };
    }
}
