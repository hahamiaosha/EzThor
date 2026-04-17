using System.Windows;
using System.Windows.Threading;
using ThorFlasher.UI.ViewModels;

namespace ThorFlasher.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void DropZone_PreviewDragOver(object sender, DragEventArgs e)
    {
        var filePaths = ExtractFilePaths(e);
        e.Effects = ViewModel.CanAcceptDrop(filePaths) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        var filePaths = ExtractFilePaths(e);
        if (ViewModel.CanAcceptDrop(filePaths))
        {
            ViewModel.HandleDroppedFile(filePaths[0]);
        }

        e.Handled = true;
    }

    private void LogTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() =>
            {
                try
                {
                    if (LogTextBox.IsKeyboardFocusWithin)
                    {
                        return;
                    }

                    LogTextBox.ScrollToEnd();
                }
                catch
                {
                    // Keep the window alive even if auto-scroll fails.
                }
            }));
    }

    private static string[] ExtractFilePaths(DragEventArgs e)
    {
        return e.Data.GetData(DataFormats.FileDrop) is string[] files
            ? files
            : Array.Empty<string>();
    }
}
