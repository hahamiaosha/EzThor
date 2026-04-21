namespace ThorFlasher.Core.Models;

public sealed class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string Level { get; set; } = "INFO";

    public string Stage { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string DisplayText => $"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Stage}: {Message}";
}
