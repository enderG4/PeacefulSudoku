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
    // ── Cell grid ───────────────────────────────────────────────────────────
    // We store all 81 cell borders in a flat array.
    // Index formula: row * 9 + col  →  e.g. row=2, col=5 → index 23
    private readonly Border[] _cells = new Border[81];

    // ── State you will fill in ──────────────────────────────────────────────
    private int _selectedIndex = -1;   // which cell is currently selected (-1 = none)
    private bool _notesMode    = false; // is notes mode on?

    // TODO: you will add the puzzle arrays here in Step 2

    public MainWindow()
    {
        InitializeComponent();
        SetupKeyboard();

        // The XAML controls (like BoardGrid) only exist after the window
        // is fully rendered for the first time. Opened fires at that point.
        Opened += (_, _) => BuildBoard();
    }

    // Shorthand: walk the full resource tree (window → app → styles)
    // this.Resources["key"] only checks the window itself and misses
    // anything declared in Assets/Styles.axaml which lives at app level.
    private IBrush R(string key) => (IBrush)this.FindResource(key)!;

    // ────────────────────────────────────────────────────────────────────────
    // BOARD CONSTRUCTION
    // Creates 81 Border controls and places them in the 9×9 UniformGrid.
    // ────────────────────────────────────────────────────────────────────────
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

    // ────────────────────────────────────────────────────────────────────────
    // KEYBOARD HANDLING
    // ────────────────────────────────────────────────────────────────────────
    private void SetupKeyboard()
    {
        KeyDown += OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // TODO Step 3: handle arrow keys, number keys, delete, 'n'
    }

    // ────────────────────────────────────────────────────────────────────────
    // EVENT HANDLERS
    // ────────────────────────────────────────────────────────────────────────

    private void Cell_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border cell && cell.Tag is int index)
            SelectCell(index);
    }

    private void NumPad_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag && int.TryParse(tag, out int number))
        {
            // TODO Step 3: call your EnterNumber(number) method
        }
    }

    private void Erase_Click(object? sender, RoutedEventArgs e)
    {
        // TODO Step 3: call your EraseCell() method
    }

    private void Notes_Click(object? sender, RoutedEventArgs e)
    {
        _notesMode = !_notesMode;
        if (_notesMode) BtnNotes.Classes.Add("on");
        else            BtnNotes.Classes.Remove("on");
    }

    private void NewGame_Click(object? sender, RoutedEventArgs e)
    {
        // TODO Step 2: call your StartNewGame() method
    }

    private void Difficulty_Click(object? sender, RoutedEventArgs e)
    {
        // TODO Step 2: read which button was clicked, update difficulty
    }

    // ────────────────────────────────────────────────────────────────────────
    // CELL SELECTION  –  highlights the selected cell + its row/col/box
    // ────────────────────────────────────────────────────────────────────────
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
                tb.Foreground = isSelected ? R("B.TextOnSel") : R("B.TextGiven");
        }
    }
}