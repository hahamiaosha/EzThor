using System.Windows;
using ThorFlasher.UI.ViewModels;

namespace ThorFlasher.UI.Views;

public partial class ProfileNameDialog : Window
{
    public ProfileNameDialog(ProfileNameDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private ProfileNameDialogViewModel ViewModel => (ProfileNameDialogViewModel)DataContext;

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.ProfileName))
        {
            MessageBox.Show(this, "Please enter a profile name.", "Missing Profile Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
