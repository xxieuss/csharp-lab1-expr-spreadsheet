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
    private int _row;
    private int _col;
	private readonly IStorage _storage = new LocalFileStorage();
    private string _currentName = "";
    private Grid _sheetGrid = new Grid();
    private Label[,] _cells = new Label[0, 0];

    private GoogleDriveStorage? _drive;

    public MainPage() {
        InitializeComponent();
        _controller.NewSheet(12, 6);
        BuildGrid();
        SelectCell(0, 0);
    }

    private void BuildGrid() {
        int rows = _controller.Sheet.Rows;
        int cols = _controller.Sheet.Cols;

        if (_cells.GetLength(1) == 0 || rows < _cells.GetLength(0) || cols < _cells.GetLength(1)) {
            RebuildGrid(rows, cols);
            return;
        }

        if (rows > _cells.GetLength(0) || cols > _cells.GetLength(1)) Grow(rows, cols);
        RefreshGrid();
    }

    private void RebuildGrid(int rows, int cols) {
        Grid grid = new Grid {
            RowSpacing = 1,
            ColumnSpacing = 1,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            BackgroundColor = Palette.GridLine
        };

        RowDefinitionCollection rowDefs = new RowDefinitionCollection();
        rowDefs.Add(new RowDefinition { Height = CellHeight });
        for (int r = 0; r < rows; r++) rowDefs.Add(new RowDefinition { Height = CellHeight });
        grid.RowDefinitions = rowDefs;

        ColumnDefinitionCollection colDefs = new ColumnDefinitionCollection();
        colDefs.Add(new ColumnDefinition { Width = HeaderWidth });
        for (int c = 0; c < cols; c++) colDefs.Add(new ColumnDefinition { Width = CellWidth });
        grid.ColumnDefinitions = colDefs;

        grid.Add(MakeHeader(""), 0, 0);
        for (int c = 0; c < cols; c++)
            grid.Add(MakeHeader(Sheet.ColumnName(c)), c + 1, 0);

            _cells = new Label[rows, cols];

        for (int r = 0; r < rows; r++) {
            grid.Add(MakeHeader((r + 1).ToString()), 0, r + 1);
            for (int c = 0; c < cols; c++) {
                Label cell = MakeCell(r, c);
                _cells[r, c] = cell;
                grid.Add(cell, c + 1, r + 1);
            }
        }

        _sheetGrid = grid;
        GridHost.Content = grid;
    }

    private void Grow(int rows, int cols) {
        int oldRows = _cells.GetLength(0);
        int oldCols = _cells.GetLength(1);

        _sheetGrid.BatchBegin();

        for (int c = oldCols; c < cols; c++) {
            _sheetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = CellWidth });
            _sheetGrid.Add(MakeHeader(Sheet.ColumnName(c)), c + 1, 0);
        }

        for (int r = oldRows; r < rows; r++) {
            _sheetGrid.RowDefinitions.Add(new RowDefinition { Height = CellHeight });
            _sheetGrid.Add(MakeHeader((r + 1).ToString()), 0, r + 1);
        }

        Label[,] grown = new Label[rows, cols];
        for (int r = 0; r < oldRows; r++)
            for (int c = 0; c < oldCols; c++)
                grown[r, c] = _cells[r, c];

        for (int r = 0; r < oldRows; r++)
            for (int c = oldCols; c < cols; c++)
                Place(grown, r, c);

        for (int r = oldRows; r < rows; r++)
            for (int c = 0; c < cols; c++)
                Place(grown, r, c);

        _cells = grown;
        _sheetGrid.BatchCommit();
    }

    private void Place(Label[,] target, int row, int col) {
        Label cell = MakeCell(row, col);
        target[row, col] = cell;
        _sheetGrid.Add(cell, col + 1, row + 1);
    }

    private Label MakeCell(int row, int col) {
        Label label = new Label {
            FontSize = 13,
            TextColor = Palette.Text,
            BackgroundColor = Palette.Cell,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(8, 0),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        TapGestureRecognizer tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => OnCellTapped(row, col);
        label.GestureRecognizers.Add(tap);

        return label;
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

    private void PaintCell(int row, int col) {
        if (row < 0 || col < 0) return;
        if (row >= _cells.GetLength(0) || col >= _cells.GetLength(1)) return;

        Cell cell = _controller.Sheet.GetCell(row, col);
        Label label = _cells[row, col];
        bool selected = row == _row && col == _col;

        label.Text = cell.Display(_controller.Mode);
        label.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
        label.HorizontalTextAlignment = _controller.Mode == DisplayMode.Expression
            ? TextAlignment.Start
            : TextAlignment.End;

        label.TextColor = cell.HasError() ? Palette.ErrorText
                        : selected        ? Palette.Accent
                        :                   Palette.Text;

        label.BackgroundColor = cell.HasError() ? Palette.CellError
                              : selected        ? Palette.CellSel
                              :                   Palette.Cell;
    }

    private void RefreshGrid() {
        for (int r = 0; r < _controller.Sheet.Rows; r++)
            for (int c = 0; c < _controller.Sheet.Cols; c++)
                PaintCell(r, c);
    }

    private void ShowFormulaBar() {
        Cell cell = _controller.Sheet.GetCell(_row, _col);
        _controller.SelectCell(cell.Name);

        CellNameLabel.Text = cell.Name;
        FormulaEntry.Text = cell.Formula;
        ErrorLabel.Text = cell.HasError()
            ? (cell.ErrorPos >= 0 ? $"{cell.ErrorText}  (позиція {cell.ErrorPos + 1})" : cell.ErrorText)
            : "";
    }

    private void SelectCell(int row, int col) {
        _row = row;
        _col = col;
        ShowFormulaBar();
        RefreshGrid();
        if (_infoView != InfoView.None) RefreshInfo();
    }

    private void OnCellTapped(int row, int col) {
        int oldRow = _row;
        int oldCol = _col;

        _row = row;
        _col = col;
        ShowFormulaBar();

        PaintCell(oldRow, oldCol);
        PaintCell(row, col);

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

    private void OnAddRow(object? sender, EventArgs e) => Resize(_controller.AddRow(_row + 1), $"Більше ніж {Sheet.MaxRows} рядків таблиця мати не може");
    private void OnRemoveRow(object? sender, EventArgs e) => Resize(_controller.RemoveRow(_row), "Таблиця не може лишитись без жодного рядка");
    private void OnAddColumn(object? sender, EventArgs e) => Resize(_controller.AddColumn(_col + 1), $"Більше ніж {Sheet.MaxCols} стовпців таблиця мати не може");
    private void OnRemoveColumn(object? sender, EventArgs e) => Resize(_controller.RemoveColumn(_col), "Таблиця не може лишитись без жодного стовпця");

    private void Resize(bool done, string problem) {
        if (!done) {
            ErrorLabel.Text = problem;
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

    private async Task<GoogleDriveStorage> GetDriveAsync() {
        if (_drive != null) return _drive;

        using Stream stream = await FileSystem.OpenAppPackageFileAsync("client_secret.json");
        using StreamReader reader = new StreamReader(stream);
        string secret = await reader.ReadToEndAsync();

        _drive = new GoogleDriveStorage(secret,
            Path.Combine(FileSystem.AppDataDirectory, "drive-token"),
            url => MainThread.BeginInvokeOnMainThread(() => Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred)));

        return _drive;
    }

    private static string NormalizeName(string name) {
        name = name.Trim();
        if (name.Length > 0 && !name.EndsWith(".json")) name += ".json";
        return name;
    }

    private async void OnDriveSave(object? sender, EventArgs e) {
        string? input = await DisplayPromptAsync("Зберегти на Google Drive", "Назва файлу:",
            initialValue: _currentName.Length > 0 ? _currentName : "таблиця.json");
        if (input == null) return;

        string name = NormalizeName(input);
        if (name.Length == 0) return;

        try {
            GoogleDriveStorage drive = await GetDriveAsync();
            await Task.Run(() => _controller.Save(drive, name));
            _currentName = name;
            await DisplayAlert("Google Drive", $"Збережено як «{name}»", "Гаразд");
        } catch (Exception ex) {
            await DisplayAlert("Google Drive", ex.Message, "Гаразд");
        }
    }

    private async void OnDriveOpen(object? sender, EventArgs e) {
        string? input = await DisplayPromptAsync("Відкрити з Google Drive", "Назва файлу:",
            initialValue: _currentName.Length > 0 ? _currentName : "таблиця.json");
        if (input == null) return;

        string name = NormalizeName(input);
        if (name.Length == 0) return;

        try {
            GoogleDriveStorage drive = await GetDriveAsync();
            await Task.Run(() => _controller.Load(drive, name));
            _currentName = name;
            BuildGrid();
            SelectCell(0, 0);
        } catch (Exception ex) {
            await DisplayAlert("Google Drive", ex.Message, "Гаразд");
        }
    }
}