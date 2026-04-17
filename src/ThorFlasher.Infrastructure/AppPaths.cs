namespace ThorFlasher.Infrastructure;

public static class AppPaths
{
    public static string AppDataRoot => EnsureDirectory(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ThorFlasher"));

    public static string ProfilesDirectory => EnsureDirectory(Path.Combine(AppDataRoot, "Profiles"));

    public static string LogsDirectory => EnsureDirectory(Path.Combine(AppDataRoot, "Logs"));

    public static string SanitizeFileName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Trim()
            .Select(character => invalidFileNameChars.Contains(character) ? '_' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "Profile" : sanitized;
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
