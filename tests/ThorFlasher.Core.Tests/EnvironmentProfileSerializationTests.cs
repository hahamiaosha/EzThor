using System.Text.Json;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Tests;

public sealed class EnvironmentProfileSerializationTests
{
    [Fact]
    public void EnvironmentProfile_RoundTripsThroughJson()
    {
        var profile = new EnvironmentProfile
        {
            ProfileName = "Lab Rig",
            HostIp = "192.168.10.1",
            TargetIp = "192.168.10.2",
            LastFilePath = @"C:\Images\firmware.bin",
            LastOperation = OperationType.Flash,
            UpdatedAt = new DateTime(2026, 4, 17, 12, 30, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(profile);
        var roundTripped = JsonSerializer.Deserialize<EnvironmentProfile>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(profile.ProfileName, roundTripped!.ProfileName);
        Assert.Equal(profile.HostIp, roundTripped.HostIp);
        Assert.Equal(profile.TargetIp, roundTripped.TargetIp);
        Assert.Equal(profile.LastFilePath, roundTripped.LastFilePath);
        Assert.Equal(profile.LastOperation, roundTripped.LastOperation);
        Assert.Equal(profile.UpdatedAt, roundTripped.UpdatedAt);
    }
}
