using ThorFlasher.Core.Services;

namespace ThorFlasher.Core.Tests;

public sealed class FileTypeResolverTests
{
    private readonly FileTypeResolver _resolver = new();

    [Theory]
    [InlineData("firmware.bin", true)]
    [InlineData("firmware.BIN", true)]
    [InlineData("capsule.cap", true)]
    [InlineData("capsule.CAP", true)]
    [InlineData("bpmp.dtb", true)]
    [InlineData("bpmp.DTB", true)]
    [InlineData("notes.txt", false)]
    [InlineData("", false)]
    public void IsSupportedPackage_ReturnsExpectedValue(string filePath, bool expected)
    {
        var result = _resolver.IsSupportedPackage(filePath);

        Assert.Equal(expected, result);
    }
}
