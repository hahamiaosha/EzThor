using System.Net;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Services;

public sealed class InputValidator
{
    private readonly FileTypeResolver _fileTypeResolver;

    public InputValidator(FileTypeResolver fileTypeResolver)
    {
        _fileTypeResolver = fileTypeResolver;
    }

    public ValidationResult Validate(OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Validate(context.HostIp, context.TargetIp, context.FilePath, context.OperationType);
    }

    public ValidationResult Validate(string? hostIp, string? targetIp, string? filePath, OperationType operationType)
    {
        var errors = new List<string>();

        if (!IsValidIpAddress(hostIp))
        {
            errors.Add("THOR Host IP must be a valid IPv4 or IPv6 address.");
        }

        if (!IsValidIpAddress(targetIp))
        {
            errors.Add("THOR Target IP must be a valid IPv4 or IPv6 address.");
        }

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            if (!File.Exists(filePath))
            {
                errors.Add("Selected firmware/capsule file does not exist.");
            }

            if (!_fileTypeResolver.IsSupportedPackage(filePath))
            {
                errors.Add("Only .bin, .cap, and .dtb files are supported.");
            }
        }

        if (operationType is not OperationType.Flash and not OperationType.CapsuleUpdate)
        {
            errors.Add("Operation type must be Flash or Capsule Update.");
        }

        return new ValidationResult(errors);
    }

    private static bool IsValidIpAddress(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value, out _);
    }
}
