using Core;
using Cell = Core.Cell;

namespace Ui;

public partial class MainPage : ContentPage {
    private const int CellWidth = 110;
    private const int CellHeight = 34;
    private const int HeaderWidth = 46;
    private enum InfoView { None, Tree, Trace, Dependencies }

    private InfoView _infoView = InfoView.None;

    private readonly SheetController _controller = new SheetController();
    private Button[,] _buttons = new Button[0, 0];
    private int _row;
    private int _col;
	private readonly IStorage _storage = new LocalFileStorage();
    private string _currentName = "";
    private Border[,] _cells = new Border[0, 0];

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

        _cells = new Border[rows, cols];

        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                Border cell = MakeCell(r, c);
                _cells[r, c] = cell;
                SheetGrid.Add(cell, c + 1, r + 1);
            }
        }

        RefreshGrid();
    }

    private Border MakeCell(int row, int col) {
        Label label = new Label {
            FontSize = 13,
            TextColor = Palette.Text,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(8, 0),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        Border border = new Border {
            Content = label,
            BackgroundColor = Palette.Cell,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            Padding = 0
        };

        TapGestureRecognizer tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => SelectCell(row, col);
        border.GestureRecognizers.Add(tap);

        return border;
    }

    private static Label MakeHeader(string text) => new Label {
        Text = text,
        FontAttributes = FontAttributes.Bold,
        FontSize = 12,
        TextColor = Palette.TextDim,
        BackgroundColor = Palette.Header,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center
    };

    private void RefreshGrid() {
        bool expressions = _controller.Mode == DisplayMode.Expression;

        for (int r = 0; r < _controller.Sheet.Rows; r++) {
            for (int c = 0; c < _controller.Sheet.Cols; c++) {
                Cell cell = _controller.Sheet.GetCell(r, c);
                Border border = _cells[r, c];
                Label label = (Label)border.Content!;

                bool selected = r == _row && c == _col;

                label.Text = cell.Display(_controller.Mode);
                label.TextColor = cell.HasError() ? Palette.ErrorText : Palette.Text;
                label.HorizontalTextAlignment = expressions ? TextAlignment.Start : TextAlignment.End;

                border.BackgroundColor = cell.HasError() ? Palette.CellError
                                       : selected        ? Palette.CellSel
                                       :                   Palette.Cell;
                border.Stroke = selected ? Palette.Accent : Colors.Transparent;
                border.StrokeThickness = selected ? 2 : 0;
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
        if (_infoView != InfoView.None) RefreshInfo();
    }

    private void OnFormulaCommitted(object? sender, EventArgs e) {
        Cell cell = _controller.Sheet.GetCell(_row, _col);
        _controller.SetCellFormula(cell, FormulaEntry.Text ?? "");

        SelectCell(Math.Min(_row + 1, _controller.Sheet.Rows - 1), _col);
        FormulaEntry.Focus();
    }

    private void OnToggleMode(object? sender, EventArgs e) {
        DisplayMode mode = _controller.Mode == DisplayMode.Value
            ? DisplayMode.Expression
            : DisplayMode.Value;

        _controller.SetDisplayMode(mode);

        bool expressions = mode == DisplayMode.Expression;
        ModeButton.Text = expressions ? "Режим: ВИРАЗ" : "Режим: ЗНАЧЕННЯ";
        ModeButton.BackgroundColor = expressions ? Palette.Accent : Palette.ButtonBg;
        ModeButton.TextColor = expressions ? Palette.Window : Palette.Text;

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

	private async void OnNew(object? sender, EventArgs e) {
        string? rowsText = await DisplayPromptAsync("Нова таблиця", "Кількість рядків:",
            initialValue: "12", keyboard: Keyboard.Numeric);
        if (rowsText == null) return;

        string? colsText = await DisplayPromptAsync("Нова таблиця", "Кількість стовпців:",
            initialValue: "6", keyboard: Keyboard.Numeric);
        if (colsText == null) return;

        if (!int.TryParse(rowsText, out int rows) || rows < 1 || rows > 500 ||
            !int.TryParse(colsText, out int cols) || cols < 1 || cols > 100) {
            await DisplayAlert("Помилка", "Розмір має бути від 1×1 до 500×100", "Гаразд");
            return;
        }

        _controller.NewSheet(rows, cols);
        _currentName = "";
        BuildGrid();
        SelectCell(0, 0);
    }

    private async void OnSave(object? sender, EventArgs e) {
        string? name = await DisplayPromptAsync("Зберегти таблицю", "Назва файлу:",
            initialValue: _currentName.Length > 0 ? _currentName : "таблиця.json");
        if (name == null) return;

        name = name.Trim();
        if (name.Length == 0) return;
        if (!name.EndsWith(".json")) name += ".json";

        string path = Path.Combine(FileSystem.AppDataDirectory, name);

        try {
            _controller.Save(_storage, path);
            _currentName = name;
            await DisplayAlert("Збережено", path, "Гаразд");
        } catch (StorageException ex) {
            await DisplayAlert("Не вдалося зберегти", ex.Message, "Гаразд");
        }
    }

    private async void OnOpen(object? sender, EventArgs e) {
        try {
            FileResult? file = await FilePicker.Default.PickAsync();
            if (file == null) return;

            _controller.Load(_storage, file.FullPath);
            _currentName = file.FileName;
            BuildGrid();
            SelectCell(0, 0);
        } catch (StorageException ex) {
            await DisplayAlert("Не вдалося відкрити", ex.Message, "Гаразд");
        } catch (Exception ex) {
            await DisplayAlert("Не вдалося відкрити", ex.Message, "Гаразд");
        }
    }

    private void OnShowTree(object? sender, EventArgs e) {
        _infoView = InfoView.Tree;
        RefreshInfo();
    }

    private void OnShowTrace(object? sender, EventArgs e) {
        _infoView = InfoView.Trace;
        RefreshInfo();
    }

    private void OnShowDependencies(object? sender, EventArgs e) {
        _infoView = InfoView.Dependencies;
        RefreshInfo();
    }

    private void OnHideInfo(object? sender, EventArgs e) {
        _infoView = InfoView.None;
        InfoPanel.IsVisible = false;
    }

    private void RefreshInfo() {
        Cell cell = _controller.Sheet.GetCell(_row, _col);

        switch (_infoView) {
            case InfoView.Tree:
                ShowTree(_controller.GetTree(cell));
                break;
            case InfoView.Trace:
                ShowTrace(_controller.GetTrace(cell));
                break;
            case InfoView.Dependencies:
                ShowDependencies(_controller.GetDependencies(cell), _controller.GetDependents(cell));
                break;
        }
    }

    private void ShowTree(Node? tree) {
        ShowInfo(tree == null
            ? $"ДЕРЕВО РОЗБОРУ {_controller.Sheet.GetCell(_row, _col).Name}\n\n"
              + "Дерева немає: клітинка порожня або містить синтаксичну помилку."
            : $"ДЕРЕВО РОЗБОРУ {_controller.Sheet.GetCell(_row, _col).Name}\n\n" + tree.ToTree(0));
    }

    private void ShowTrace(List<string> steps) {
        string title = $"ПОКРОКОВЕ ОБЧИСЛЕННЯ {_controller.Sheet.GetCell(_row, _col).Name}\n\n";
        ShowInfo(steps.Count == 0
            ? title + "Обчислювати нічого: клітинка порожня або містить синтаксичну помилку."
            : title + string.Join("\n", steps));
    }

    private void ShowDependencies(List<Cell> precedents, List<Cell> dependents) {
        string from = precedents.Count == 0
            ? "немає"
            : string.Join(", ", precedents.Select(c => c.Name));
        string to = dependents.Count == 0
            ? "немає"
            : string.Join(", ", dependents.Select(c => c.Name));

        ShowInfo($"ЗАЛЕЖНОСТІ {_controller.Sheet.GetCell(_row, _col).Name}\n\n"
                 + $"Залежить від: {from}\n"
                 + $"Від неї залежать: {to}");
    }

    private void ShowInfo(string text) {
        InfoLabel.Text = text;
        InfoPanel.IsVisible = true;
    }
}