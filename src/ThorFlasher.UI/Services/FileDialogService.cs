using Microsoft.Win32;

namespace ThorFlasher.UI.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? BrowseForFirmwareFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Firmware or Capsule File",
            Filter = "Firmware or Capsule (*.bin;*.cap)|*.bin;*.cap|Binary Images (*.bin)|*.bin|Capsule Files (*.cap)|*.cap",
            Multiselect = false,
            CheckFileExists = true,
            CheckPathExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
