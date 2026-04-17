using System.Text.Json;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Infrastructure.Persistence;

public sealed class JsonLogStore : ILogStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task SaveLogAsync(string logName, IReadOnlyList<LogEntry> entries, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logName);
        ArgumentNullException.ThrowIfNull(entries);

        var path = Path.Combine(AppPaths.LogsDirectory, $"{AppPaths.SanitizeFileName(logName)}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, entries, SerializerOptions, token).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LogEntry>> LoadLogAsync(string logName, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logName);

        var path = Path.Combine(AppPaths.LogsDirectory, $"{AppPaths.SanitizeFileName(logName)}.json");
        if (!File.Exists(path))
        {
            return Array.Empty<LogEntry>();
        }

        await using var stream = File.OpenRead(path);
        var entries = await JsonSerializer.DeserializeAsync<List<LogEntry>>(stream, SerializerOptions, token).ConfigureAwait(false);
        return entries is not null ? entries : Array.Empty<LogEntry>();
    }

    public Task<IReadOnlyList<string>> ListLogsAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();

        IReadOnlyList<string> results = Directory
            .EnumerateFiles(AppPaths.LogsDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray()!;

        return Task.FromResult(results);
    }
}
