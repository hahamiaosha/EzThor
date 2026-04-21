using System.Windows;
using ThorFlasher.UI.ViewModels;

namespace ThorFlasher.UI.Views;

public partial class ProfilePickerWindow : Window
{
    public ProfilePickerWindow(ProfilePickerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private ProfilePickerViewModel ViewModel => (ProfilePickerViewModel)DataContext;

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedProfile is null)
        {
            MessageBox.Show(this, "Select a profile to load.", "No Profile Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
