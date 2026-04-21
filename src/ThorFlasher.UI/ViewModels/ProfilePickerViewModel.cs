using System.Collections.ObjectModel;
using ThorFlasher.Core.Models;

namespace ThorFlasher.UI.ViewModels;

public sealed class ProfilePickerViewModel : ViewModelBase
{
    private EnvironmentProfile? _selectedProfile;

    public ProfilePickerViewModel(IEnumerable<EnvironmentProfile> profiles)
    {
        Profiles = new ObservableCollection<EnvironmentProfile>(profiles);
        SelectedProfile = Profiles.FirstOrDefault();
    }

    public ObservableCollection<EnvironmentProfile> Profiles { get; }

    public EnvironmentProfile? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }
}
