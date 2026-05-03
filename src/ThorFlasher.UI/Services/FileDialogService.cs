using Microsoft.Win32;

namespace ThorFlasher.UI.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? BrowseForFirmwareFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Firmware, Capsule, or DTB File",
            Filter = "Firmware, Capsule, or DTB (*.bin;*.cap;*.dtb)|*.bin;*.cap;*.dtb|Binary Images (*.bin)|*.bin|Capsule Files (*.cap)|*.cap|DTB Files (*.dtb)|*.dtb",
            Multiselect = false,
            CheckFileExists = true,
            CheckPathExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
