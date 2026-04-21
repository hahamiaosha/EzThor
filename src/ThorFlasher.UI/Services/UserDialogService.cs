using System.Windows;
using ThorFlasher.Core.Models;
using ThorFlasher.UI.ViewModels;
using ThorFlasher.UI.Views;

namespace ThorFlasher.UI.Services;

public sealed class UserDialogService : IUserDialogService
{
    public bool ConfirmOperation(OperationType operationType, string targetIp, string filePath)
    {
        var operationName = operationType switch
        {
            OperationType.Flash => "Flash",
            OperationType.CapsuleUpdate => "Capsule Update",
            _ => "Unknown Operation"
        };

        var message = $"You are about to upload {filePath} to target {targetIp} and run {operationName}. Continue?";
        return ShowMessageBox(message, "Confirm Operation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    public void ShowInformation(string title, string message)
    {
        ShowMessageBox(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string title, string message)
    {
        ShowMessageBox(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public string? PromptForProfileName(string? initialName)
    {
        var viewModel = new ProfileNameDialogViewModel
        {
            ProfileName = initialName ?? string.Empty
        };

        var window = new ProfileNameDialog(viewModel)
        {
            Owner = GetOwner()
        };

        return window.ShowDialog() == true
            ? viewModel.ProfileName.Trim()
            : null;
    }

    public EnvironmentProfile? PickProfile(IReadOnlyList<EnvironmentProfile> profiles)
    {
        var viewModel = new ProfilePickerViewModel(profiles);
        var window = new ProfilePickerWindow(viewModel)
        {
            Owner = GetOwner()
        };

        return window.ShowDialog() == true ? viewModel.SelectedProfile : null;
    }

    private static Window? GetOwner()
    {
        return Application.Current?.MainWindow;
    }

    private static MessageBoxResult ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
    {
        var owner = GetOwner();
        return owner is null
            ? MessageBox.Show(message, title, buttons, image)
            : MessageBox.Show(owner, message, title, buttons, image);
    }
}
