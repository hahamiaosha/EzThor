using System.Windows.Media;

namespace ThorFlasher.UI.Views;

public static class LogEntryTextStyle
{
    public static Color DefaultColor { get; } = Color.FromRgb(0x1F, 0x29, 0x37);

    public static Color ErrorColor { get; } = Color.FromRgb(0xB4, 0x23, 0x18);

    private static readonly Brush DefaultBrush = CreateBrush(DefaultColor);
    private static readonly Brush ErrorBrush = CreateBrush(ErrorColor);

    public static Brush GetForegroundBrush(string? level)
    {
        return string.Equals(level, "ERROR", StringComparison.OrdinalIgnoreCase)
            ? ErrorBrush
            : DefaultBrush;
    }

    private static Brush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
