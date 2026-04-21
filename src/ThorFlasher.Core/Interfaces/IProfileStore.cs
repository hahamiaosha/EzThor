using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Interfaces;

public interface IProfileStore
{
    Task SaveAsync(EnvironmentProfile profile, CancellationToken token = default);

    Task<IReadOnlyList<EnvironmentProfile>> GetAllAsync(CancellationToken token = default);

    Task<EnvironmentProfile?> LoadAsync(string profileName, CancellationToken token = default);
}
