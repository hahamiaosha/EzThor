using ThorFlasher.Core.Services;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Tests;

public sealed class InputValidatorTests
{
    private readonly InputValidator _validator = new(new FileTypeResolver());

    [Theory]
    [InlineData(".bin")]
    [InlineData(".cap")]
    public void Validate_ReturnsSuccess_ForValidInputs(string extension)
    {
        var path = CreateTempFile(extension);

        try
        {
            var result = _validator.Validate("192.168.1.10", "192.168.1.20", path, OperationType.Flash);

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Validate_ReturnsSuccess_WhenFileIsOmitted()
    {
        var result = _validator.Validate("192.168.1.10", "192.168.1.20", string.Empty, OperationType.Flash);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenHostIpIsInvalid()
    {
        var path = CreateTempFile(".bin");

        try
        {
            var result = _validator.Validate("invalid-host", "192.168.1.20", path, OperationType.Flash);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("Host IP", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenTargetIpIsInvalid()
    {
        var path = CreateTempFile(".cap");

        try
        {
            var result = _validator.Validate("192.168.1.10", "bad-target", path, OperationType.Flash);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("Target IP", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenFileDoesNotExist()
    {
        var result = _validator.Validate("192.168.1.10", "192.168.1.20", Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.bin"), OperationType.Flash);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenExtensionIsUnsupported()
    {
        var path = CreateTempFile(".txt");

        try
        {
            var result = _validator.Validate("192.168.1.10", "192.168.1.20", path, OperationType.Flash);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains(".bin, .cap, and .dtb", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Validate_ReturnsFailure_WhenOperationTypeIsNone()
    {
        var path = CreateTempFile(".bin");

        try
        {
            var result = _validator.Validate("192.168.1.10", "192.168.1.20", path, OperationType.None);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("Operation type", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempFile(string extension)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
        File.WriteAllText(path, "test");
        return path;
    }
}
