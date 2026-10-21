using Core;
using Cell = Core.Cell;

namespace Ui;

public partial class MainPage : ContentPage {
    private const int CellWidth = 110;
    private const int CellHeight = 34;
    private const int HeaderWidth = 46;

    private readonly SheetController _controller = new SheetController();
    private Button[,] _buttons = new Button[0, 0];
    private int _row;
    private int _col;

    public MainPage() {
        InitializeComponent();
        _controller.NewSheet(12, 6);
        BuildGrid();
        SelectCell(0, 0);
    }

    private void BuildGrid() {
        SheetGrid.Clear();
        SheetGrid.RowDefinitions.Clear();
        SheetGrid.ColumnDefinitions.Clear();

        int rows = _controller.Sheet.Rows;
        int cols = _controller.Sheet.Cols;

        SheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = HeaderWidth });
        for (int c = 0; c < cols; c++)
            SheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = CellWidth });

        SheetGrid.RowDefinitions.Add(new RowDefinition { Height = CellHeight });
        for (int r = 0; r < rows; r++)
            SheetGrid.RowDefinitions.Add(new RowDefinition { Height = CellHeight });

        SheetGrid.Add(MakeHeader(""), 0, 0);

        for (int c = 0; c < cols; c++)
            SheetGrid.Add(MakeHeader(_controller.Sheet.GetCell(0, c).Name.TrimEnd('1')), c + 1, 0);

        for (int r = 0; r < rows; r++)
            SheetGrid.Add(MakeHeader((r + 1).ToString()), 0, r + 1);

        _buttons = new Button[rows, cols];

        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                int row = r;
                int col = c;

                Button button = new Button {
                    BackgroundColor = Colors.White,
                    TextColor = Colors.Black,
                    FontSize = 13,
                    Padding = 0,
                    CornerRadius = 0,
                    BorderWidth = 0
                };
                button.Clicked += (s, e) => SelectCell(row, col);

                _buttons[r, c] = button;
                SheetGrid.Add(button, c + 1, r + 1);
            }
        }

        RefreshGrid();
    }

    private static Label MakeHeader(string text) => new Label {
        Text = text,
        FontAttributes = FontAttributes.Bold,
        FontSize = 12,
        TextColor = Colors.Black,
        BackgroundColor = Color.FromArgb("#DDDDDD"),
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    private void RefreshGrid() {
        for (int r = 0; r < _controller.Sheet.Rows; r++) {
            for (int c = 0; c < _controller.Sheet.Cols; c++) {
                Cell cell = _controller.Sheet.GetCell(r, c);
                Button button = _buttons[r, c];

                button.Text = cell.Display(_controller.Mode);
                button.TextColor = cell.HasError() ? Colors.Firebrick : Colors.Black;
                button.BackgroundColor = (r == _row && c == _col)
                    ? Color.FromArgb("#CCE4FF")
                    : Colors.White;
            }
        }
    }

    private void SelectCell(int row, int col) {
        _row = row;
        _col = col;

        Cell cell = _controller.Sheet.GetCell(row, col);
        _controller.SelectCell(cell.Name);

        CellNameLabel.Text = cell.Name;
        FormulaEntry.Text = cell.Formula;
        ErrorLabel.Text = cell.HasError()
            ? (cell.ErrorPos >= 0 ? $"{cell.ErrorText}  (позиція {cell.ErrorPos + 1})" : cell.ErrorText)
            : "";

        RefreshGrid();
    }

    private void OnFormulaCommitted(object? sender, EventArgs e) {
        Cell cell = _controller.Sheet.GetCell(_row, _col);
        _controller.SetCellFormula(cell, FormulaEntry.Text ?? "");
        SelectCell(_row, _col);
    }

    private void OnToggleMode(object? sender, EventArgs e) {
        DisplayMode mode = _controller.Mode == DisplayMode.Value
            ? DisplayMode.Expression
            : DisplayMode.Value;

        _controller.SetDisplayMode(mode);
        ModeButton.Text = mode == DisplayMode.Value ? "Режим: ЗНАЧЕННЯ" : "Режим: ВИРАЗ";
        RefreshGrid();
    }

    private void OnAddRow(object? sender, EventArgs e) => Resize(_controller.AddRow(_row + 1));
    private void OnRemoveRow(object? sender, EventArgs e) => Resize(_controller.RemoveRow(_row));
    private void OnAddColumn(object? sender, EventArgs e) => Resize(_controller.AddColumn(_col + 1));
    private void OnRemoveColumn(object? sender, EventArgs e) => Resize(_controller.RemoveColumn(_col));

    private void Resize(bool done) {
        if (!done) {
            ErrorLabel.Text = "Так зробити не можна: таблиця не може лишитись без рядків або стовпців";
            return;
        }
        BuildGrid();
        SelectCell(Math.Min(_row, _controller.Sheet.Rows - 1),
                   Math.Min(_col, _controller.Sheet.Cols - 1));
    }
}