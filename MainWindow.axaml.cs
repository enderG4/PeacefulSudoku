using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;
using System;
using Avalonia.Animation;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Linq;

namespace PeacefulSudoku;

public partial class MainWindow : Window
{
    
    private readonly TextBlock[]   _numberLabels = new TextBlock[81];
    private readonly TextBlock[,]  _noteLabels   = new TextBlock[81, 9];
    private readonly Notes[]       _notes        = new Notes[81];

    // Cell grid

    private readonly Border[] _cells = new Border[81];
    private int _selectedIndex = -1;   // which cell is currently selected (-1 = none)
    private bool _notesMode    = false; // is notes mode on?

    private int[,] _puzzle   = new int[9, 9];
    private int[,] _solution = new int[9, 9];
    private bool[,] _given   = new bool[9, 9];
    private Difficulty _difficulty = Difficulty.Medium;

    private DispatcherTimer _gameTimer  = new();
    private int             _elapsedSeconds = 0;

    public MainWindow()
    {
        InitializeComponent();
        SetupKeyboard();

        Opened += (_, _) =>
        {
            BuildBoard();
            StartNewGame();
            FillAllButOne(); // remove this when done testing
        };
    }

    private IBrush R(string key) => (IBrush)this.FindResource(key)!;

    private void BuildBoard()
    {
        for (int row = 0; row < 9; row++)
        for (int col = 0; col < 9; col++)
        {
            int index = row * 9 + col;

            // -- Main number label
            var numberLabel = new TextBlock
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                FontSize            = 22,
                FontWeight          = FontWeight.Medium,
                Foreground          = R("B.TextGiven"),
                IsVisible           = true
            };
            _numberLabels[index] = numberLabel;

            // -- 3x3 note grid with 9 small labels
            var noteGrid = new UniformGrid { Rows = 3, Columns = 3, IsVisible = false };
            for (int n = 0; n < 9; n++)
            {
                var noteLabel = new TextBlock
                {
                    Text = (n + 1).ToString(),
                    FontSize = 11,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                    Foreground          = R("B.TextNote"),
                    IsVisible           = false
                };
                _noteLabels[index, n] = noteLabel;
                noteGrid.Children.Add(noteLabel);
            }

            // -- Container holds both, only one visible at a time
            var container = new Grid();
            container.Children.Add(noteGrid);
            container.Children.Add(numberLabel);

            var cell = new Border
            {
                Child           = container,
                Background      = R("B.Cell"),
                BorderBrush     = R("B.BoxBorder"),
                BorderThickness = GetCellBorder(row, col),
                Cursor          = new Cursor(StandardCursorType.Hand),
                Tag             = index,
                Transitions     = new Transitions
                {
                    new BrushTransition
                    {
                        Property = Border.BackgroundProperty,
                        Duration = TimeSpan.FromMilliseconds(180)
                    }
                }
            };

            cell.PointerPressed += Cell_PointerPressed;
            _cells[index] = cell;
            BoardGrid.Children.Add(cell);
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
            Key.D1 or Key.NumPad1  => 1,
            Key.D2 or Key.NumPad2  => 2,
            Key.D3 or Key.NumPad3  => 3,
            Key.D4 or Key.NumPad4  => 4,
            Key.D5 or Key.NumPad5  => 5,
            Key.D6 or Key.NumPad6  => 6,
            Key.D7 or Key.NumPad7  => 7,
            Key.D8 or Key.NumPad8  => 8,
            Key.D9 or Key.NumPad9  => 9,
            Key.Delete or Key.Back => 0,
            _                      => -1
        };

        if (num >= 0) EnterNumber(num);

        switch (e.Key)
        {
            case Key.Up:    MoveSelection(-9); break;
            case Key.Down:  MoveSelection(+9); break;
            case Key.Left:  MoveSelection(-1); break;
            case Key.Right: MoveSelection(+1); break;
            case Key.N:     ToggleNotes();     break;
        }   
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

    private void ToggleNotes()
    {
        _notesMode = !_notesMode;
        if (_notesMode) BtnNotes.Classes.Add("on");
        else            BtnNotes.Classes.Remove("on");
    }

    private void Notes_Click(object? sender, RoutedEventArgs e) => ToggleNotes();

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

            if (_given[row, col])
                _numberLabels[i].Foreground = isSelected ? R("B.TextOnSel") : R("B.TextGiven");
            else if (_puzzle[row, col] != 0 && _puzzle[row, col] != _solution[row, col])
                _numberLabels[i].Foreground = isSelected ? R("B.TextOnSel") : R("B.TextError");
            else
                _numberLabels[i].Foreground = isSelected ? R("B.TextOnSel") : R("B.TextPlayer");

            IBrush noteBrush = isSelected ? R("B.TextOnSel") : R("B.TextNote");
            for (int n = 0; n < 9; n++)
                _noteLabels[i, n].Foreground = noteBrush;
        }
    }

    private void StartNewGame()
    {
        StopTimer();
        var (puzzle, solution) = SudokuGenerator.Generate(_difficulty);
        LoadPuzzle(puzzle, solution);
        StartTimer();
    }

    private void LoadPuzzle(int[,] puzzle, int[,] solution)
{
    _solution      = solution;
    _selectedIndex = -1;

    for (int row = 0; row < 9; row++)
    for (int col = 0; col < 9; col++)
    {
        int index = row * 9 + col;
        int value = puzzle[row, col];

        _puzzle[row, col] = value;
        _given[row, col]  = value != 0;
        _notes[index].Clear();

        _cells[index].Background        = R("B.Cell");
        _numberLabels[index].Text       = value == 0 ? "" : value.ToString();
        _numberLabels[index].Foreground = R("B.TextGiven");
        _numberLabels[index].FontWeight = value != 0 ? FontWeight.SemiBold : FontWeight.Medium;
        _numberLabels[index].IsVisible  = true;

        // hide all note labels
        for (int n = 0; n < 9; n++)
            _noteLabels[index, n].IsVisible = false;

        // hide note grid
        var container = (Grid)_cells[index].Child!;
        ((UniformGrid)container.Children[0]).IsVisible = false;
    }
}

    private void EnterNumber(int number)
    {
        if (_selectedIndex < 0) return;

        int row = _selectedIndex / 9;
        int col = _selectedIndex % 9;

        if (_given[row, col]) return;

        if (_notesMode && number != 0)
        {
            // can't add notes to a cell that already has a number
            if (_puzzle[row, col] != 0) return;

            _notes[_selectedIndex].Toggle(number);
            RefreshNoteDisplay(_selectedIndex);
        }
        else
        {
            _puzzle[row, col] = number;
            _notes[_selectedIndex].Clear();

            var container  = (Grid)_cells[_selectedIndex].Child!;
            var noteGrid   = (UniformGrid)container.Children[0];
            noteGrid.IsVisible = false;

            _numberLabels[_selectedIndex].IsVisible  = true;
            _numberLabels[_selectedIndex].Text       = number == 0 ? "" : number.ToString();
            _numberLabels[_selectedIndex].Foreground = number == 0          ? R("B.TextPlayer")
                                                    : _solution[row, col] == number ? R("B.TextPlayer")
                                                    : R("B.TextError");

            // clear all note labels
            for (int n = 0; n < 9; n++)
                _noteLabels[_selectedIndex, n].IsVisible = false;

            UpdateHighlights();

            if (number != 0) CheckWin();
        }
    }

    private void RefreshNoteDisplay(int index)
    {
        var container = (Grid)_cells[index].Child!;
        var noteGrid  = (UniformGrid)container.Children[0];

        bool hasNotes = !_notes[index].IsEmpty;
        noteGrid.IsVisible              = hasNotes;
        _numberLabels[index].IsVisible  = !hasNotes;

        for (int n = 0; n < 9; n++)
            _noteLabels[index, n].IsVisible = _notes[index].Has(n + 1);
    }

    private void CheckWin()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
                if (_puzzle[row, col] != _solution[row, col])
                    return; // not done yet

        PlayWinAnimation();
    }

    private void PlayWinAnimation()
    {
        // diagonal wave order: cells closer to top-left go first
        var order = Enumerable.Range(0, 81)
            .OrderBy(i => (i / 9) + (i % 9))
            .ToList();

        var sweep = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(18) };
        int step  = 0;

        sweep.Tick += (_, _) =>
        {
            if (step < order.Count)
            {
                _cells[order[step]].Background = R("B.Selected");
                step++;
            }
            else
            {
                sweep.Stop();

                // pause, then sweep back to normal
                var pause = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
                pause.Tick += (_, _) =>
                {
                    pause.Stop();
                    SweepBack(order);
                };
                pause.Start();
            }
        };

        sweep.Start();
    }

    private void SweepBack(List<int> order)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(18) };
        int step  = 0;

        timer.Tick += async (_, _) =>
        {
            if (step < order.Count)
            {
                _cells[order[step]].Background = R("B.Cell");
                step++;
            }
            else
            {
                timer.Stop();

                string time       = TimerLabel.Text ?? "00:00";
                string difficulty = _difficulty.ToString().ToLower();

                var dialog = new WinDialog(time, difficulty);
                var result = await dialog.ShowDialog<bool?>(this);

                if (result == true)
                    StartNewGame();
            }
        };

        timer.Start();
    }

    private void StartTimer()
    {
        _elapsedSeconds = 0;
        _gameTimer.Interval = TimeSpan.FromSeconds(1);
        _gameTimer.Tick += (_, _) =>
        {
            _elapsedSeconds++;
            int h = _elapsedSeconds / 3600;
            int m = (_elapsedSeconds % 3600) / 60;
            int s = _elapsedSeconds % 60;

            TimerLabel.Text = h > 0
                ? $"{h}:{m:D2}:{s:D2}"
                : $"{m:D2}:{s:D2}";
        };
        _gameTimer.Start();
    }

    private void StopTimer()
    {
        _gameTimer.Stop();
        _gameTimer = new DispatcherTimer(); // reset so next game gets a fresh one
    }

    private void FillAllButOne()
    {
        for (int row = 0; row < 9; row++)
            for (int col = 0; col < 9; col++)
            {
                // skip the very last empty cell
                if (row == 8 && col == 8) continue;
                if (_given[row, col])    continue;

                int correct = _solution[row, col];
                _puzzle[row, col] = correct;

                _numberLabels[row * 9 + col].Text       = correct.ToString();
                _numberLabels[row * 9 + col].Foreground = R("B.TextPlayer");
                _numberLabels[row * 9 + col].IsVisible  = true;
            }
    }

    private void MoveSelection(int delta)
    {
        if (_selectedIndex < 0)
        {
            SelectCell(0); // nothing selected yet, start at top-left
            return;
        }

        int row = _selectedIndex / 9;
        int col = _selectedIndex % 9;

        int newRow = row + delta / 9;  // -1, 0, or +1
        int newCol = col + delta % 9;  // -1, 0, or +1

        // clamp to board bounds
        if (newRow < 0 || newRow > 8 || newCol < 0 || newCol > 8) return;

        SelectCell(newRow * 9 + newCol);
    }
}