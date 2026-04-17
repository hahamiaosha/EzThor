using System.Text.Json;
using ThorFlasher.Core.Interfaces;
using ThorFlasher.Core.Models;

namespace ThorFlasher.Infrastructure.Persistence;

public sealed class JsonProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public async Task SaveAsync(EnvironmentProfile profile, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(profile.ProfileName);

        var path = Path.Combine(AppPaths.ProfilesDirectory, $"{AppPaths.SanitizeFileName(profile.ProfileName)}.json");

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, profile, SerializerOptions, token).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<EnvironmentProfile>> GetAllAsync(CancellationToken token = default)
    {
        var profiles = new List<EnvironmentProfile>();

        foreach (var file in Directory.EnumerateFiles(AppPaths.ProfilesDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            token.ThrowIfCancellationRequested();

            await using var stream = File.OpenRead(file);
            var profile = await JsonSerializer.DeserializeAsync<EnvironmentProfile>(stream, SerializerOptions, token).ConfigureAwait(false);
            if (profile is not null)
            {
                profiles.Add(profile);
            }
        }

        return profiles
            .OrderByDescending(profile => profile.UpdatedAt)
            .ThenBy(profile => profile.ProfileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<EnvironmentProfile?> LoadAsync(string profileName, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileName);

        var path = Path.Combine(AppPaths.ProfilesDirectory, $"{AppPaths.SanitizeFileName(profileName)}.json");
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<EnvironmentProfile>(stream, SerializerOptions, token).ConfigureAwait(false);
    }
}
