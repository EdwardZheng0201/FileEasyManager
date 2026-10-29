using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using GongSolutions.Wpf.DragDrop;

namespace FileEasyManager;

public partial class MainWindow
{
    // -------------------- Quick Move state --------------------
    // WPF drag/drop pattern adapted from Microsoft Learn:
    // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/drag-and-drop-overview
    private const string QuickMoveFormat = "FileEasyManager.PreviewItems";
    private readonly List<Popup> movePanels = [];
    private string? hoverPath;
    private readonly DispatcherTimer hoverTimer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private FrameworkElement? pendingAnchor;
    private MoveTarget? pendingTarget;
    private string? droppedPath;
    private record MoveTarget(string Path, int Level);

    // -------------------- Gong drag handler --------------------
    // https://github.com/punker76/gong-wpf-dragdrop
    // https://github.com/punker76/gong-wpf-dragdrop/wiki/Usage
    private QuickMoveSource? quickMoveSource;
    public DefaultDragHandler QuickMoveDragHandler => quickMoveSource ??= new QuickMoveSource(this);
    private sealed class QuickMoveSource(MainWindow window) : DefaultDragHandler
    {
        public override void StartDrag(IDragInfo info)
        {
            base.StartDrag(info);
            var items = info.SourceItems.Cast<PreviewItem>().ToArray();
            info.Data = items;
            info.DataFormat = DataFormats.GetDataFormat(QuickMoveFormat);
            info.Effects = items.Length > 0 ? DragDropEffects.Move : DragDropEffects.None;
            window.droppedPath = null;
            window.Feedback.Text = "Drag to a folder. Hold to expand; Esc to cancel.";
        }
        public override void DragDropOperationFinished(DragDropEffects result, IDragInfo info)
        {
            window.hoverTimer.Stop();
            window.pendingAnchor = null; window.pendingTarget = null;
            window.CloseMovePanels(0);
            window.hoverPath = null;
            string? destination = window.droppedPath;
            window.Feedback.Text = destination == null ? "Drag cancelled." : "Example destination: " + destination;

        }
    }
    // -------------------- Sidebar Drag Targets --------------------
    private void Move_SidebarOver(object sender, DragEventArgs e)
    {
        var button = (Button)sender;
        HoverMoveTarget(button, new MoveTarget((string)button.Tag, 0), e);
    }
    private void Move_SidebarDrop(object sender, DragEventArgs e) => FinishMove((string)((Button)sender).Tag, e);
    private bool CanMoveTo(string path, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(QuickMoveFormat) || path is "Home" or "This PC") return false;
        var items = (PreviewItem[])e.Data.GetData(QuickMoveFormat);
        return items.All(item => item.Parent != path && (!item.Folder ||
            (!path.Equals(item.Path, StringComparison.OrdinalIgnoreCase) &&
             !path.StartsWith(item.Path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))));
    }

    // -------------------- Folder hover --------------------
    private void HoverMoveTarget(FrameworkElement anchor, MoveTarget target, DragEventArgs e)
    {
        e.Handled = true;
        bool valid = CanMoveTo(target.Path, e);
        e.Effects = valid ? DragDropEffects.Move : DragDropEffects.None;
        if (!e.Data.GetDataPresent(QuickMoveFormat)) return;
        // Browsing a folder remains possible even when dropping into it is invalid.
        if (target.Path is "Home" or "This PC") { CloseMovePanels(0); hoverPath = null; return; }
        string key = target.Level + ":" + target.Path;
        if (hoverPath != key)
        {
            hoverPath = key;
            hoverTimer.Stop();
            pendingAnchor = anchor; pendingTarget = target;
            hoverTimer.Tick -= Move_HoverTick;
            hoverTimer.Tick += Move_HoverTick;
            hoverTimer.Start();
            CloseMovePanels(target.Level);
        }
        Feedback.Text = valid ? "Release to preview move to " + target.Path : "Cannot drop here; hover to browse its subfolders.";

    }
    // A timer opens the next panel even when the pointer stops moving.
    private void Move_HoverTick(object? sender, EventArgs e)
    {
        hoverTimer.Stop();
        if (pendingAnchor != null && pendingTarget != null && movePanels.Count == pendingTarget.Level)
            OpenMovePanel(pendingAnchor, pendingTarget);
    }
    private void Move_TargetLeave(object sender, DragEventArgs e)
    {
        if (ReferenceEquals(sender, pendingAnchor))
        {
            hoverTimer.Stop();
            pendingAnchor = null; pendingTarget = null; hoverPath = null;
        }
    }
    private void OpenMovePanel(FrameworkElement anchor, MoveTarget target)
    {
        var contents = new StackPanel();
        contents.Children.Add(new TextBlock { Text = target.Path, Margin = new Thickness(6) });
        contents.Children.Add(new Separator());
        var children = samples.Where(item => item.Parent == target.Path).OrderByDescending(item => item.Folder).ThenBy(item => item.Name).ToList();
        foreach (var item in children)
        {
            var row = new Button { Content = item.Name + (item.Folder ? "   >" : ""), Padding = new Thickness(6), HorizontalContentAlignment = HorizontalAlignment.Left, AllowDrop = true };
            row.DragOver += (_, e) =>
            {
                if (item.Folder) HoverMoveTarget(row, new MoveTarget(item.Path, target.Level + 1), e);
                else { hoverTimer.Stop(); e.Effects = DragDropEffects.None; e.Handled = true; CloseMovePanels(target.Level + 1); hoverPath = null; }
            };
            row.DragLeave += Move_TargetLeave;
            row.Drop += (_, e) => { if (item.Folder) FinishMove(item.Path, e); else { e.Effects = DragDropEffects.None; e.Handled = true; } };
            contents.Children.Add(row);
        }
        if (children.Count == 0) contents.Children.Add(new TextBlock { Text = "(Empty folder)", Margin = new Thickness(6) });
        var border = new Border
        {
            Background = SystemColors.WindowBrush, BorderBrush = SystemColors.ActiveBorderBrush, BorderThickness = new Thickness(1),
            Child = new ScrollViewer { Content = contents, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 400 },
            Width = 240, AllowDrop = true
        };
        border.DragOver += (_, e) => { e.Effects = CanMoveTo(target.Path, e) ? DragDropEffects.Move : DragDropEffects.None; e.Handled = true; };
        border.Drop += (_, e) => FinishMove(target.Path, e);
        var popup = new Popup { PlacementTarget = anchor, Placement = PlacementMode.Right, StaysOpen = true, Child = border };
        movePanels.Add(popup);
        popup.IsOpen = true;
    }

    // -------------------- Drop Preview and Panel Cleanup --------------------
    private void FinishMove(string path, DragEventArgs e)
    {
        e.Handled = true;
        bool valid = CanMoveTo(path, e);
        // UI preview only: no data is transferred. Return None so Gong closes its
        // drag preview for this native WPF target (its own DropHandler is not used).
        // Gong 4.0.0 DragDrop.cs: DragSourceOnMouseMove cleanup after DoDragDrop.
        e.Effects = DragDropEffects.None;
        if (valid) droppedPath = path;
    }
    private void CloseMovePanels(int level)
    {
        while (movePanels.Count > level)
        {
            movePanels[^1].IsOpen = false;
            movePanels.RemoveAt(movePanels.Count - 1);
        }
    }
}

