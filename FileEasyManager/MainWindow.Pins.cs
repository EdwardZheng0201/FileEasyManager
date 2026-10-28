using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace FileEasyManager;

public partial class MainWindow
{
    // -------------------- Pinned Locations - UI Example State --------------------
    // Binding pattern: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/how-to-create-and-bind-to-an-observablecollection
    // Pins are shortcuts to sample folders, kept for this preview session only.
    public ObservableCollection<PreviewItem> PinnedLocations { get; } = [];

    // -------------------- Add Folder Shortcuts Without Duplicates --------------------
    private void PinFolders(IEnumerable<PreviewItem> folders)
    {
        int added = 0;
        foreach (var folder in folders.Where(x => x.Folder))
        {
            if (PinnedLocations.Any(x => x.Path == folder.Path)) continue;
            PinnedLocations.Add(folder);
            added++;
        }
        Feedback.Text = added > 0 ? $"Pinned {added} example folder(s)." : "Already pinned, or no folder selected.";
    }
    private void PinCurrent_Click(object sender, RoutedEventArgs e) =>
        PinFolders(samples.Where(x => x.Folder && x.Path == currentPath));

    // -------------------- Remove a Shortcut Only --------------------
    private void Unpin_Click(object sender, RoutedEventArgs e)
    {
        if (((ContextMenu)sender).PlacementTarget is not Button { DataContext: PreviewItem folder }) return;
        PinnedLocations.Remove(folder);
        Feedback.Text = "Unpinned " + folder.Name + ". The example folder is unchanged.";
    }

    // -------------------- Pin Menu Availability --------------------
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
        pin.IsEnabled = folders.Any(x => x.Folder && !PinnedLocations.Any(p => p.Path == x.Path));
    }
}
