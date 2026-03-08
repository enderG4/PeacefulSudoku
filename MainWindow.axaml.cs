using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using System;

namespace PeacefulSudoku;

public partial class MainWindow : Window
{
    // Cell grid
    private readonly Border[] _cells = new Border[81];
    private int _selectedIndex = -1;   // which cell is currently selected (-1 = none)
    private bool _notesMode    = false; // is notes mode on?

    private int[,] _puzzle   = new int[9, 9];
    private int[,] _solution = new int[9, 9];
    private bool[,] _given   = new bool[9, 9];
    private Difficulty _difficulty = Difficulty.Medium;

    public MainWindow()
    {
        InitializeComponent();
        SetupKeyboard();

        Opened += (_, _) => BuildBoard();
    }

    private IBrush R(string key) => (IBrush)this.FindResource(key)!;

    private void BuildBoard()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                int index = row * 9 + col;

                // -- Inner content: a TextBlock for the number
                var number = new TextBlock
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                    FontSize            = 22,
                    FontWeight          = FontWeight.Medium,
                    Foreground          = R("B.TextGiven"),
                    Text                = ""
                };

                // -- The cell itself is a Border wrapping the TextBlock
                var cell = new Border
                {
                    Child           = number,
                    Background      = R("B.Cell"),
                    BorderBrush     = R("B.BoxBorder"),
                    BorderThickness = GetCellBorder(row, col),
                    Cursor          = new Cursor(StandardCursorType.Hand),
                    Tag             = index
                };

                cell.PointerPressed += Cell_PointerPressed;

                _cells[index] = cell;
                BoardGrid.Children.Add(cell);
            }
        }
    }

    // Returns a Thickness that draws thicker lines at the 3x3 box boundaries.
    private static Thickness GetCellBorder(int row, int col)
    {
        double left   = col % 3 == 0 ? 3.0 : 1.0;
        double top    = row % 3 == 0 ? 3.0 : 1.0;
        double right  = col == 8     ? 2.0 : 0.0;
        double bottom = row == 8     ? 2.0 : 0.0;
        return new Thickness(left, top, right, bottom);
    }

    private void SetupKeyboard()
    {
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        int num = e.Key switch
        {
            Key.D1 or Key.NumPad1    => 1,
            Key.D2 or Key.NumPad2    => 2,
            Key.D3 or Key.NumPad3    => 3,
            Key.D4 or Key.NumPad4    => 4,
            Key.D5 or Key.NumPad5    => 5,
            Key.D6 or Key.NumPad6    => 6,
            Key.D7 or Key.NumPad7    => 7,
            Key.D8 or Key.NumPad8    => 8,
            Key.D9 or Key.NumPad9    => 9,
            Key.Delete or Key.Back   => 0,
            _                        => -1   // anything else → ignore
        };

        if (num >= 0) EnterNumber(num);
    }

    // Event Handlers
    private void Cell_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border cell && cell.Tag is int index)
            SelectCell(index);
    }

    private void NumPad_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int number))
            EnterNumber(number);
    }

    private void Erase_Click(object? sender, RoutedEventArgs e)
    {
        EnterNumber(0);
    }

    private void Notes_Click(object? sender, RoutedEventArgs e)
    {
        _notesMode = !_notesMode;
        if (_notesMode) BtnNotes.Classes.Add("on");
        else            BtnNotes.Classes.Remove("on");
    }

    private void NewGame_Click(object? sender, RoutedEventArgs e)
    {
        StartNewGame();
    }

    private void Difficulty_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button clicked) return;

        // remove "active" from all four pills, add it to the clicked one
        BtnEasy.Classes.Remove("active");
        BtnMedium.Classes.Remove("active");
        BtnHard.Classes.Remove("active");
        BtnExpert.Classes.Remove("active");
        clicked.Classes.Add("active");

        _difficulty = clicked.Name switch
        {
            "BtnEasy"   => Difficulty.Easy,
            "BtnMedium" => Difficulty.Medium,
            "BtnHard"   => Difficulty.Hard,
            "BtnExpert" => Difficulty.Expert,
            _           => Difficulty.Medium
        };

    }

    //highlight the selected cell, its row, column and box
    private void SelectCell(int index)
    {
        _selectedIndex = index;
        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        if (_selectedIndex < 0) return;

        int selRow = _selectedIndex / 9;
        int selCol = _selectedIndex % 9;

        for (int i = 0; i < 81; i++)
        {
            int row = i / 9;
            int col = i % 9;

            bool isSelected = i == _selectedIndex;
            bool sameRow    = row == selRow;
            bool sameCol    = col == selCol;
            bool sameBox    = (row / 3 == selRow / 3) && (col / 3 == selCol / 3);

            var cell = _cells[i];

            cell.Background = isSelected              ? R("B.Selected")  :
                              sameRow || sameCol || sameBox ? R("B.Highlight") :
                                                              R("B.Cell"); 

            if (cell.Child is TextBlock tb)
            {
                if (isSelected)
                    tb.Foreground = R("B.TextOnSel");
                else if (_given[row, col])
                    tb.Foreground = R("B.TextGiven");
                else if (_puzzle[row, col] != 0 && _puzzle[row, col] != _solution[row, col])
                    tb.Foreground = R("B.TextError");
                else
                    tb.Foreground = R("B.TextPlayer");
            }
        }
    }

    private void StartNewGame()
    {
        var (puzzle, solution) = SudokuGenerator.Generate(_difficulty);
        LoadPuzzle(puzzle, solution);
    }

    private void LoadPuzzle(int[,] puzzle, int[,] solution)
    {
        _solution = solution;
        _selectedIndex = -1;

        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                int index = row * 9 + col;
                int value = puzzle[row, col];

                _puzzle[row, col] = value;
                _given[row, col]  = value != 0;

                var cell = _cells[index];
                cell.Background = R("B.Cell");

                if (cell.Child is TextBlock tb)
                {
                    tb.Text       = value == 0 ? "" : value.ToString();
                    tb.Foreground = R("B.TextGiven");
                    tb.FontWeight = value != 0 ? FontWeight.SemiBold : FontWeight.Medium;
                }
            }
    }

    private void EnterNumber(int number)
    {
        if (_selectedIndex < 0) return;           // no cell selected, do nothing

        int row = _selectedIndex / 9;
        int col = _selectedIndex % 9;

        if (_given[row, col]) return;             // can't overwrite a given digit

        _puzzle[row, col] = number;

        var cell = _cells[_selectedIndex];
        if (cell.Child is TextBlock tb)
        {
            tb.Text       = number == 0 ? "" : number.ToString();
            tb.Foreground = _solution[row, col] == number ? R("B.TextPlayer") : R("B.TextError");
        }

        UpdateHighlights();
    }
}