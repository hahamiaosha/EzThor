using System.Windows;
using System.Windows.Controls;

namespace ThorFlasher.UI.Controls;

public sealed class StatusBadge : Control
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(string.Empty));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
