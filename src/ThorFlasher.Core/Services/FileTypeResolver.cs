using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Services;

public sealed class FileTypeResolver
{
    public bool IsSupportedPackage(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        return GetNormalizedExtension(filePath) is ".bin" or ".cap";
    }

    public string GetNormalizedExtension(string? filePath)
    {
        return string.IsNullOrWhiteSpace(filePath)
            ? string.Empty
            : Path.GetExtension(filePath).ToLowerInvariant();
    }
}
