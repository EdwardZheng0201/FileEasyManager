using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FileEasyManager;

public partial class MainWindow
{
    // -------------------- Search controls --------------------
    // https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/datepicker
    private void SearchOptions_Click(object sender, RoutedEventArgs e) => SearchFilters.IsOpen = !SearchFilters.IsOpen;
    private void SearchPreview_Click(object sender, RoutedEventArgs e)
    {
        SearchFilters.IsOpen = false;
        Feedback.Text = "Search is not implemented in this example.";
    }
    private void SearchName_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        SearchPreview_Click(sender, e);
        e.Handled = true;
    }

    // -------------------- Reset filters --------------------
    private void SearchReset_Click(object sender, RoutedEventArgs e)
    {
        SearchNameBox.Clear();
        SearchScope.SelectedIndex = SearchKind.SelectedIndex = SearchType.SelectedIndex = SearchMatch.SelectedIndex = SearchSizeUnit.SelectedIndex = 0;
        SearchSubfolders.IsChecked = true;
        SearchCase.IsChecked = false;
        SearchDateFrom.SelectedDate = SearchDateTo.SelectedDate = null;
        SearchDateFrom.Text = SearchDateTo.Text = "";
        SearchMinSize.Clear();
        SearchMaxSize.Clear();
        Feedback.Text = "Search options reset.";
    }
    private void SearchDate_Invalid(object? sender, DatePickerDateValidationErrorEventArgs e)
    {
        e.ThrowException = false;
        Feedback.Text = "Choose a valid date using the calendar.";
    }
}
