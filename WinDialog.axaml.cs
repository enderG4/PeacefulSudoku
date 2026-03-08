using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PeacefulSudoku;

public partial class WinDialog : Window
{
    public WinDialog() => InitializeComponent(); // required by Avalonia

    public WinDialog(string time, string difficulty) : this()
    {
        TimeLabel.Text = time;
        DiffLabel.Text = difficulty;
    }

    private void NewGame_Click(object? sender, RoutedEventArgs e) => Close(true);
    private void Quit_Click(object? sender, RoutedEventArgs e) => Close(false);
}