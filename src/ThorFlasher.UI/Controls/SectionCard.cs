using System.Windows;
using System.Windows.Controls;

namespace ThorFlasher.UI.Controls;

public sealed class SectionCard : HeaderedContentControl
{
    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(
            nameof(Subtitle),
            typeof(string),
            typeof(SectionCard),
            new PropertyMetadata(string.Empty));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }
}
