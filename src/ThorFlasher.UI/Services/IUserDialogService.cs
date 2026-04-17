using ThorFlasher.Core.Models;

namespace ThorFlasher.UI.Services;

public interface IUserDialogService
{
    bool ConfirmOperation(OperationType operationType, string targetIp, string filePath);

    void ShowInformation(string title, string message);

    void ShowError(string title, string message);

    string? PromptForProfileName(string? initialName);

    EnvironmentProfile? PickProfile(IReadOnlyList<EnvironmentProfile> profiles);
}
