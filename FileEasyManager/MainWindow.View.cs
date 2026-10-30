using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace FileEasyManager;

public partial class MainWindow
{
    // -------------------- Toolbar menus --------------------
    private GridView? detailsView;
    private string viewMode = "Details";
    private void ToolbarMenu_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    // -------------------- Sorting --------------------
    // Shares ApplySort with the column header handler.
    private void SortMenu_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not MenuItem { Tag: string value }) return;
        if (value is "Ascending" or "Descending")
            sortDirection = Enum.Parse<ListSortDirection>(value);
        else sortProperty = value;
        ApplySort();
    }
    private void UpdateSortChecks()
    {
        foreach (var item in SortButton.ContextMenu.Items.OfType<MenuItem>())
            item.IsChecked = (string)item.Tag == sortProperty || (string)item.Tag == sortDirection.ToString();
    }

    // -------------------- View modes --------------------
    private void ViewMenu_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not MenuItem { Tag: string mode }) return;
        if (mode == "FolderSizes")
        {
            var menuItem = (MenuItem)e.OriginalSource;

            SizeColumn.DisplayMemberBinding = new Binding(
                menuItem.IsChecked ? "SizeWithFolders" : "SizeText");

            return;
        }
        viewMode = mode;
        FileList.View = null;
        FileList.ItemTemplate = null;
        if (mode == "Details")
        {
            FileList.ItemsPanel = (ItemsPanelTemplate)FindResource("RowsPanel");
            FileList.View = detailsView;
        }
        else
        {
            FileList.ItemTemplate = (DataTemplate)FindResource((mode == "List" ? "Small" : mode) + "Template");
            FileList.ItemsPanel = (ItemsPanelTemplate)FindResource(mode == "List" ? "ListPanel" : "IconsPanel");
        }
        ScrollViewer.SetHorizontalScrollBarVisibility(FileList, mode is "List" or "Details" ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(FileList, mode == "List" ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto);
        foreach (var item in ViewButton.ContextMenu.Items.OfType<MenuItem>())
        {
            if (item.Tag as string == "FolderSizes") continue;

            item.IsChecked = (string)item.Tag == viewMode;
        }
        Feedback.Text = "View: " + ((MenuItem)e.OriginalSource).Header;
    }
}
