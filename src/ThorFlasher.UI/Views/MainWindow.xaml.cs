using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ThorFlasher.UI.ViewModels;

namespace ThorFlasher.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Allow drag-and-drop even when the app is running elevated.
        EnableDragDropForElevatedProcess();
    }

    private void EnableDragDropForElevatedProcess()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        // WM_DROPFILES = 0x0233, WM_COPYDATA = 0x004A, WM_COPYGLOBALDATA = 0x0049
        ChangeWindowMessageFilterEx(hwnd, 0x0233, 1 /* MSGFLT_ALLOW */, IntPtr.Zero);
        ChangeWindowMessageFilterEx(hwnd, 0x004A, 1, IntPtr.Zero);
        ChangeWindowMessageFilterEx(hwnd, 0x0049, 1, IntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilterEx(IntPtr hwnd, uint message, uint action, IntPtr changeInfo);

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
