namespace FileEasyManager;

public partial class MainWindow
{
    // -------------------- Close programs dialog --------------------
    private void ShowCloseProgramsPreview(PreviewItem? folder)
    {
        if (folder?.Folder != true) return;
        var dialog = new CloseProgramsDialog(folder.Path) { Owner = this };
        Feedback.Text = dialog.ShowDialog() == true
            ? "Example: end tasks confirmed."
            : "Cancelled.";
    }
}
