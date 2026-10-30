using System.Windows;
using System.Windows.Controls;

namespace FileEasyManager;

public partial class MainWindow
{
    // -------------------- Console State --------------------
    private string consoleShell = "CMD";

    // -------------------- Console Menu --------------------
    private void ConsoleMenu_Click(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not MenuItem { Tag: string shell })
            return;

        consoleShell = shell;

        foreach (var item in ConsoleButton.ContextMenu.Items.OfType<MenuItem>())
        {
            item.IsChecked = item.Tag as string == consoleShell;
        }

        Feedback.Text = "Command environment: " + consoleShell;
    }

    // -------------------- Command Preview --------------------
    private void ShowConsolePreview()
    {
        string command = AddressBox.Text.Trim();
        if (command.Length == 0) return;

        string location = currentPath is "Home" or "This PC"
            ? "C:\\"
            : currentPath;

        if (consoleShell == "WSL" && location.Length >= 3 && location[1] == ':')
        {
            location = "/mnt/" +
                char.ToLowerInvariant(location[0]) +
                location[2..].Replace('\\', '/');
        }

        string prompt = consoleShell switch
        {
            "PowerShell" => "PS " + location + "> ",
            "WSL" => "user@example:" + location + "$ ",
            _ => location + ">"
        };

        ConsoleOutput.Text =
            prompt + command + Environment.NewLine +
            "Example command execution." + Environment.NewLine +
            Environment.NewLine +
            prompt;

        AddressBox.Visibility = Visibility.Collapsed;
        ConsoleExitButton.Visibility = Visibility.Visible;
        ConsoleButton.IsEnabled = false;

        ConsolePopup.IsOpen = true;
        Feedback.Text = "Command preview.";
    }

    // -------------------- Console Exit --------------------
    private void ConsoleExit_Click(object sender, RoutedEventArgs e)
    {
        ExitConsole();
    }

    private void ExitConsole()
    {
        ConsolePopup.IsOpen = false;

        ConsoleExitButton.Visibility = Visibility.Collapsed;
        AddressBox.Visibility = Visibility.Visible;
        ConsoleButton.IsEnabled = true;

        AddressBox.Text = currentPath;
        AddressBox.ToolTip = "Enter a folder path or command and press Enter";
    }
}