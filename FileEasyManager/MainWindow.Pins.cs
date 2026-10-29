using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace FileEasyManager;

public partial class MainWindow
{
    // -------------------- Pinned folders --------------------
    public ObservableCollection<PreviewItem> PinnedLocations { get; } = [];

    // -------------------- Add pins --------------------
    private void PinFolders(IEnumerable<PreviewItem> folders)
    {
        int added = 0;
        foreach (var folder in folders.Where(x => x.Folder))
        {
            if (PinnedLocations.Any(x => x.Path == folder.Path)) continue;
            PinnedLocations.Add(folder);
            added++;
        }
        Feedback.Text = added > 0 ? $"Pinned {added} folder(s)." : "No new folders to pin.";
    }

    // -------------------- Remove pins --------------------
    private void Unpin_Click(object sender, RoutedEventArgs e)
    {
        if (((ContextMenu)sender).PlacementTarget is not Button { DataContext: PreviewItem folder }) return;
        if (e.OriginalSource is MenuItem { Tag: "ClosePrograms" }) { ShowCloseProgramsPreview(folder); return; }
        PinnedLocations.Remove(folder);
        Feedback.Text = "Unpinned " + folder.Name + ".";
    }

    // -------------------- Context menu --------------------
    private void PinMenu_Opened(object sender, RoutedEventArgs e)
    {
        var menu = (ContextMenu)sender;
        var pin = menu.Items.OfType<MenuItem>().FirstOrDefault(x => x.Tag as string == "Pin");
        if (pin == null) return;
        IEnumerable<PreviewItem> folders = menu.PlacementTarget switch
        {
            ListView list => list.SelectedItems.Cast<PreviewItem>(),
            Button button => samples.Where(x => x.Path == button.Tag as string),
            _ => []
        };
        var selection = folders.ToArray();
        var close = menu.Items.OfType<MenuItem>().FirstOrDefault(x => x.Tag as string == "ClosePrograms");
        if (close != null) close.Visibility = selection.Length == 1 && selection[0].Folder ? Visibility.Visible : Visibility.Collapsed;
        pin.IsEnabled = selection.Any(x => x.Folder && !PinnedLocations.Any(p => p.Path == x.Path));
    }
}
