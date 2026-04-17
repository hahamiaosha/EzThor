using System.Windows;

namespace ThorFlasher.UI.Services;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Clipboard.SetText(text);
    }
}
