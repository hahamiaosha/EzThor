using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Services;

public sealed class FileTypeResolver
{
    public OperationType Resolve(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return OperationType.Unknown;
        }

        var extension = Path.GetExtension(filePath);

        return extension.ToLowerInvariant() switch
        {
            ".bin" => OperationType.BinFlash,
            ".cap" => OperationType.CapUpdate,
            _ => OperationType.Unknown
        };
    }
}
