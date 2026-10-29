using System.Windows;

namespace FileEasyManager;

public partial class CloseProgramsDialog : Window
{
    // -------------------- Example tasks --------------------
    public CloseProgramsDialog(string folder)
    {
        InitializeComponent();
        FolderPath.Text = folder;
        ExampleTasks.ItemsSource = new[]
        {
            new { Name = "ExampleFile1.docx", Application = "ExampleEditor.exe", State = "Open", Path = folder.TrimEnd('\\') + "\\ExampleFile1.docx" },
            new { Name = "ExampleFile2.txt", Application = "ExampleNotepad.exe", State = "Open", Path = folder.TrimEnd('\\') + "\\ExampleFile2.txt" },
            new { Name = "ExampleProgram1.exe", Application = "ExampleProgram1.exe", State = "Running", Path = folder.TrimEnd('\\') + "\\ExampleProgram1.exe" }
        };
    }

    // -------------------- Confirmation --------------------
    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
