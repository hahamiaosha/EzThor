namespace ThorFlasher.Adapters.Helpers;

internal static class ScriptPathTranslator
{
    public static string TranslateIfNeeded(string path, bool translateWindowsPathsForWsl, string? shellExecutable = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (translateWindowsPathsForWsl)
        {
            return TranslateToWslPath(path);
        }

        if (UsesBashShell(shellExecutable))
        {
            return TranslateToBashFriendlyPath(path);
        }

        return path;
    }

    private static string TranslateToWslPath(string path)
    {
        const string wslPrefix = @"\\wsl.localhost\";
        if (path.StartsWith(wslPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path[wslPrefix.Length..].Replace('\\', '/');
            var slashIndex = remainder.IndexOf('/');
            return slashIndex >= 0
                ? remainder[slashIndex..]
                : "/";
        }

        if (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
        {
            var drive = char.ToLowerInvariant(path[0]);
            var remainder = path[2..].Replace('\\', '/');
            return $"/mnt/{drive}{remainder}";
        }

        return path.Replace('\\', '/');
    }

    private static string TranslateToBashFriendlyPath(string path)
    {
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return path.Replace('\\', '/');
        }

        if (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
        {
            return $"{char.ToUpperInvariant(path[0])}:{path[2..].Replace('\\', '/')}";
        }

        return path.Replace('\\', '/');
    }

    private static bool UsesBashShell(string? shellExecutable)
    {
        if (string.IsNullOrWhiteSpace(shellExecutable))
        {
            return false;
        }

        var fileName = Path.GetFileName(shellExecutable);
        return fileName.Equals("bash.exe", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("bash", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("sh.exe", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("sh", StringComparison.OrdinalIgnoreCase);
    }
}
