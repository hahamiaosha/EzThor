namespace ThorFlasher.UI.ViewModels;

public sealed class ProfileNameDialogViewModel : ViewModelBase
{
    private string _profileName = string.Empty;

    public string ProfileName
    {
        get => _profileName;
        set => SetProperty(ref _profileName, value);
    }
}
