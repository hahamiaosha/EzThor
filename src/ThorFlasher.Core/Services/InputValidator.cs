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

        return Validate(context.HostIp, context.TargetIp, context.FilePath);
    }

    public ValidationResult Validate(string? hostIp, string? targetIp, string? filePath)
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

        if (string.IsNullOrWhiteSpace(filePath))
        {
            errors.Add("Firmware/Capsule file path is required.");
        }
        else
        {
            if (!File.Exists(filePath))
            {
                errors.Add("Selected firmware/capsule file does not exist.");
            }

            if (_fileTypeResolver.Resolve(filePath) == OperationType.Unknown)
            {
                errors.Add("Only .bin and .cap files are supported.");
            }
        }

        return new ValidationResult(errors);
    }

    private static bool IsValidIpAddress(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value, out _);
    }
}
