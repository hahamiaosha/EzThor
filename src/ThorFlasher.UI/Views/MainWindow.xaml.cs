using System.Collections.Specialized;
using System.Windows;
using ThorFlasher.UI.ViewModels;

namespace ThorFlasher.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LogEntries.CollectionChanged += OnLogEntriesCollectionChanged;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.LogEntries.CollectionChanged -= OnLogEntriesCollectionChanged;
        base.OnClosed(e);
    }

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

    private void OnLogEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (LogListBox.Items.Count == 0)
        {
            return;
        }

        var lastItem = LogListBox.Items[LogListBox.Items.Count - 1];
        LogListBox.ScrollIntoView(lastItem);
    }

    private static string[] ExtractFilePaths(DragEventArgs e)
    {
        return e.Data.GetData(DataFormats.FileDrop) is string[] files
            ? files
            : Array.Empty<string>();
    }
}
