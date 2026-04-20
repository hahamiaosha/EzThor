using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ThorFlasher.UI.ViewModels;

namespace ThorFlasher.UI.Views.Pages;

public partial class SetupPage : UserControl
{
    private bool _isDropZoneHighlighted;

    public SetupPage()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void DropZone_PreviewDragOver(object sender, DragEventArgs e)
    {
        var filePaths = ExtractFilePaths(e);
        var canAccept = ViewModel.CanAcceptDrop(filePaths);
        e.Effects = canAccept ? DragDropEffects.Copy : DragDropEffects.None;
        SetDropZoneHighlight(canAccept, filePaths.Length > 0);
        e.Handled = true;
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        var filePaths = ExtractFilePaths(e);
        SetDropZoneHighlight(ViewModel.CanAcceptDrop(filePaths), filePaths.Length > 0);
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        ResetDropZoneHighlight();
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        var filePaths = ExtractFilePaths(e);
        if (ViewModel.CanAcceptDrop(filePaths))
        {
            ViewModel.HandleDroppedFile(filePaths[0]);
        }

        ResetDropZoneHighlight();
        e.Handled = true;
    }

    private static string[] ExtractFilePaths(DragEventArgs e)
    {
        return e.Data.GetData(DataFormats.FileDrop) is string[] files
            ? files
            : Array.Empty<string>();
    }

    private void SetDropZoneHighlight(bool canAcceptDrop, bool hasDragData)
    {
        if (!hasDragData && !canAcceptDrop)
        {
            return;
        }

        _isDropZoneHighlighted = true;
        DropZoneBorder.BorderBrush = (Brush)FindResource(canAcceptDrop ? "Brush.Accent" : "Brush.Error");
        DropZoneBorder.Background = (Brush)FindResource(canAcceptDrop ? "Brush.AccentMuted" : "Brush.ErrorMuted");
    }

    private void ResetDropZoneHighlight()
    {
        if (!_isDropZoneHighlighted)
        {
            return;
        }

        _isDropZoneHighlighted = false;
        DropZoneBorder.BorderBrush = (Brush)FindResource("Brush.Border");
        DropZoneBorder.Background = (Brush)FindResource("Brush.SurfaceAlt");
    }
}
