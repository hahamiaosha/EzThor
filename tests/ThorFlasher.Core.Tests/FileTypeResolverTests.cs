using ThorFlasher.Core.Models;
using ThorFlasher.Core.Services;

namespace ThorFlasher.Core.Tests;

public sealed class FileTypeResolverTests
{
    private readonly FileTypeResolver _resolver = new();

    [Theory]
    [InlineData("firmware.bin", OperationType.BinFlash)]
    [InlineData("firmware.BIN", OperationType.BinFlash)]
    [InlineData("capsule.cap", OperationType.CapUpdate)]
    [InlineData("capsule.CAP", OperationType.CapUpdate)]
    [InlineData("notes.txt", OperationType.Unknown)]
    [InlineData("", OperationType.Unknown)]
    public void Resolve_ReturnsExpectedOperationType(string filePath, OperationType expected)
    {
        var result = _resolver.Resolve(filePath);

        Assert.Equal(expected, result);
    }
}
