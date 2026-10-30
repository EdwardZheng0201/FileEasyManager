using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FileEasyManager;

public partial class MainWindow : Window
{
    // -------------------- Navigation State --------------------
    private string currentPath = "Home";
    private readonly Stack<string> back = new();
    private readonly Stack<string> forward = new();
    private string sortProperty = "Name";
    private ListSortDirection sortDirection = ListSortDirection.Ascending;

    // -------------------- Example data --------------------
    private readonly List<PreviewItem> samples =
    [
        new("C:", "This PC", true), new("D:", "This PC", true),
        new("Desktop", "C:\\", true), new("Downloads", "C:\\", true),
        new("Documents", "C:\\", true), new("Pictures", "C:\\", true),
        new("Music", "C:\\", true), new("Videos", "C:\\", true),
        new("ExampleFolder1", "C:\\Documents", true, 322122547200L),
        new("ExampleFolder2", "C:\\Documents", true,  314572800),
        new("ExampleFolder3", "C:\\Documents\\ExampleFolder1", true, 104857600),
        new("ExampleFolder4", "C:\\Documents\\ExampleFolder1", true, 104857600),
        new("ExampleFile1.docx", "C:\\Documents\\ExampleFolder1", false, 102400, new DateTime(2026, 10, 20)),
        new("ExampleFile2.docx", "C:\\Documents", false, 204800, new DateTime(2026, 10, 27)),
        new("ExampleFile3.pdf", "C:\\Documents", false, 307200, new DateTime(2026, 10, 22)),
        new("ExampleFile4.txt", "C:\\Documents", false, 409600, new DateTime(2026, 10, 25)),
        new("ExampleFile5.png", "C:\\Documents", false, 512000, new DateTime(2026, 10, 24)),
        new("ExampleFolder5", "D:\\", true), new("ExampleFolder6", "D:\\", true)
    ];

    // -------------------- Startup and Page Switching --------------------
    public MainWindow()
    {
        InitializeComponent();
        detailsView = FileList.View as GridView;
        RecentFolders.ItemsSource = samples.Where(x => x.Folder && x.Parent != "This PC").Skip(6).Take(5).ToList();
        RecentFiles.ItemsSource = samples.Where(x => !x.Folder).Take(5).ToList();
        PinnedLocations.Add(samples.First(x => x.Name == "ExampleFolder1"));
        PinnedLocations.Add(samples.First(x => x.Name == "ExampleFolder5"));
        ShowPage();
    }
    private void Navigate(string path)
    {
        if (path == currentPath) return;
        back.Push(currentPath);
        forward.Clear();
        currentPath = path;
        ShowPage();
    }
    private void ShowPage()
    {
        bool home = currentPath == "Home";
        HomeView.Visibility = home ? Visibility.Visible : Visibility.Collapsed;
        FolderView.Visibility = home ? Visibility.Collapsed : Visibility.Visible;
        FileToolbar.Visibility = Visibility.Visible;
        SortButton.IsEnabled = ViewButton.IsEnabled = !home;
        AddressBox.Text = currentPath;
        FolderTitle.Text = currentPath;
        FileList.ItemsSource = samples.Where(x => x.Parent == currentPath).ToList();
        EmptyLabel.Visibility = FileList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        BackButton.IsEnabled = back.Count > 0;
        ForwardButton.IsEnabled = forward.Count > 0;
        UpButton.IsEnabled = currentPath is not "Home" and not "This PC";
        ApplySort();
        UpdateSelection();
    }

    // -------------------- Navigation Buttons and Address Bar --------------------
    private void Location_Click(object sender, RoutedEventArgs e) => Navigate((string)((Button)sender).Tag);
    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (back.Count == 0) return;
        forward.Push(currentPath); currentPath = back.Pop(); ShowPage();
    }
    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (forward.Count == 0) return;
        back.Push(currentPath); currentPath = forward.Pop(); ShowPage();
    }
    private void Up_Click(object sender, RoutedEventArgs e)
    {
        var folder = samples.FirstOrDefault(x => x.Folder && x.Path == currentPath);
        if (folder != null) Navigate(folder.Parent);
    }
    private void Refresh_Click(object sender, RoutedEventArgs e) => ShowPage();
    private void Address_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        string path = AddressBox.Text.Trim();
        var folder = samples.FirstOrDefault(x => x.Folder && x.Path.TrimEnd('\\').Equals(path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
        if (path.Equals("Home", StringComparison.OrdinalIgnoreCase)) Navigate("Home");
        else if (path.Equals("This PC", StringComparison.OrdinalIgnoreCase)) Navigate("This PC");
        else if (folder != null) Navigate(folder.Path);
        else Feedback.Text = "Demo path not found.";
        e.Handled = true;
    }

    // -------------------- Selection and Opening --------------------
    private void Selection_Changed(object sender, SelectionChangedEventArgs e) => UpdateSelection();
    private void UpdateSelection()
    {
        OpenButton.IsEnabled = RenameButton.IsEnabled = FileList.SelectedItems.Count == 1;
        CopyButton.IsEnabled = CutButton.IsEnabled = MoveButton.IsEnabled = DeleteButton.IsEnabled = FileList.SelectedItems.Count > 0;
        ItemCount.Text = currentPath == "Home" ? "Home" : $"{FileList.Items.Count} items, {FileList.SelectedItems.Count} selected";
    }
    private void OpenItem(PreviewItem? item)
    {
        if (item == null) return;
        if (item.Folder) Navigate(item.Path);
        else Preview("Open " + item.Name);
    }
    private void Files_DoubleClick(object sender, MouseButtonEventArgs e) => Recent_DoubleClick(sender, e);
    private void Recent_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        var list = (ListView)sender;
        if (ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) is ListViewItem)
            OpenItem(list.SelectedItem as PreviewItem);
    }
    private void List_RightClick(object sender, MouseButtonEventArgs e)
    {
        var list = (ListView)sender;
        var row = ItemsControl.ContainerFromElement(list, e.OriginalSource as DependencyObject) as ListViewItem;
        if (row == null) list.UnselectAll();
        else if (!row.IsSelected) { list.UnselectAll(); row.IsSelected = true; }
    }

    // -------------------- Column Sorting - Microsoft Learn Reference --------------------
    // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-sort-a-gridview-column-when-a-header-is-clicked
    private void Sort_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not GridViewColumnHeader header || header.Column == null) return;
        string property = header.Column.Header.ToString() switch { "Type" => "Type", "Date modified" => "ModifiedDate", "Size" => "Bytes", _ => "Name" };
        sortDirection = property == sortProperty && sortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        sortProperty = property; ApplySort();
    }
    private void ApplySort()
    {
        var view = CollectionViewSource.GetDefaultView(FileList.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription("Folder", ListSortDirection.Descending));
        view.SortDescriptions.Add(new SortDescription(sortProperty, sortDirection));
        string label = sortProperty switch { "ModifiedDate" => "Date modified", "Bytes" => "Size", _ => sortProperty };
        SortLabel.Text = $"{label} {(sortDirection == ListSortDirection.Ascending ? "↑" : "↓")}";
        UpdateSortChecks();
    }

    // -------------------- UI Action Previews - No File Operations --------------------
    private void Preview(string action) => MessageBox.Show(this, action + " is not implemented in this example.", "FileEasyManager");
    private void Action_Click(object sender, RoutedEventArgs e)
    {
        string action = (string)((Button)sender).Tag;
        if (action == "Open") OpenItem(FileList.SelectedItem as PreviewItem);
        else Preview(action);
    }
    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not MenuItem item || item.HasItems) return;
        string action = item.Tag as string ?? item.Header.ToString()!;
        var list = ((ContextMenu)sender).PlacementTarget as ListView ?? FileList;
        if (action == "Pin") PinFolders(list?.SelectedItems.Cast<PreviewItem>() ?? []);
        else if (action == "ClosePrograms") ShowCloseProgramsPreview(list?.SelectedItem as PreviewItem);
        else if (action == "Open") OpenItem(list?.SelectedItem as PreviewItem);
        else if (action == "Refresh") ShowPage();
        else Preview(action);
    }
    private void LocationMenu_Click(object sender, RoutedEventArgs e)
    {
        if (((ContextMenu)sender).PlacementTarget is not Button button) return;
        if (e.OriginalSource is MenuItem { Tag: "Pin" }) PinFolders(samples.Where(x => x.Path == (string)button.Tag));
        else if (e.OriginalSource is MenuItem { Tag: "ClosePrograms" }) ShowCloseProgramsPreview(samples.FirstOrDefault(x => x.Path == (string)button.Tag));
        else Navigate((string)button.Tag);
    }
    private void Files_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { OpenItem(FileList.SelectedItem as PreviewItem); e.Handled = true; }
    }
}

// -------------------- Simple Display Model --------------------
public record PreviewItem(string Name, string Parent, bool Folder, long Bytes = 0, DateTime? ModifiedAt = null)
{
    public string Path => Parent == "This PC" ? Name + "\\" : Parent.TrimEnd('\\') + "\\" + Name;
    public string Icon => Folder ? "\uE8B7" : "\uE8A5";
    public string Type => Folder ? "File folder" : System.IO.Path.GetExtension(Name) + " file";
    public DateTime ModifiedDate => ModifiedAt ?? new DateTime(2026, 10, 27, 10, 30, 0);
    public string Modified => ModifiedDate.ToString("dd MMM yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
    public string SizeText => Folder ? "—" : $"{Bytes / 1024d:0} KB";

    public string SizeWithFolders => FormatSize(Bytes);

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (1024d * 1024 * 1024):0.##} GB";

        if (bytes >= 1024L * 1024)
            return $"{bytes / (1024d * 1024):0.##} MB";

        if (bytes >= 1024L)
            return $"{bytes / 1024d:0.##} KB";

        return $"{bytes} B";
    }
}
