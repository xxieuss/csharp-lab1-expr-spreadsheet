namespace Core;

public class Sheet {
    private const int MaxColumnLetters = 3;

    private readonly List<List<Cell>> _rows = new List<List<Cell>>();

    public int Rows => _rows.Count;
    public int Cols => _rows.Count == 0 ? 0 : _rows[0].Count;

    public Sheet(int rows, int cols) {
        for (int r = 0; r < rows; r++) {
            List<Cell> row = new List<Cell>();
            for (int c = 0; c < cols; c++) row.Add(new Cell());
            _rows.Add(row);
        }
        Rename();
    }

    public Cell GetCell(int row, int col) => _rows[row][col];

    public Cell GetCell(string name) {
        (int row, int col) = ParseName(name);
        if (row >= Rows || col >= Cols)
            throw new EvalException($"Клітинка {name} поза межами таблиці ({Rows}×{Cols})");
        return _rows[row][col];
    }

    public Value GetValue(string name) {
        Cell cell = GetCell(name);
        if (cell.HasError()) throw new EvalException($"Клітинка {name} містить помилку");
        if (cell.Value.Kind == ValueKind.Empty) throw new EvalException($"Клітинка {name} порожня");
        return cell.Value;
    }

    public void SetValue(Cell cell, Value value) {
        cell.Value = value;
        cell.ErrorText = "";
        cell.ErrorPos = -1;
    }

    private void Rename() {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                _rows[r][c].Name = ColumnName(c) + (r + 1);
    }

    public static string ColumnName(int col) {
        string name = "";
        col++;
        while (col > 0) {
            name = (char)('A' + (col - 1) % 26) + name;
            col = (col - 1) / 26;
        }
        return name;
    }

    private static (int row, int col) ParseName(string name) {
        int i = 0;
        int col = 0;
        while (i < name.Length && char.IsLetter(name[i])) {
            if (i == MaxColumnLetters) throw new EvalException($"Некоректне посилання '{name}'");
            col = col * 26 + (char.ToUpperInvariant(name[i]) - 'A' + 1);
            i++;
        }
        if (i == 0 || i == name.Length) throw new EvalException($"Некоректне посилання '{name}'");
        if (!int.TryParse(name.Substring(i), out int row) || row < 1)
            throw new EvalException($"Некоректне посилання '{name}'");
        return (row - 1, col - 1);
    }

    public void AddRow(int index) {
        List<Cell> row = new List<Cell>();
        for (int c = 0; c < Cols; c++) row.Add(new Cell());
        _rows.Insert(index, row);
        Rename();
    }

    public void RemoveRow(int index) {
        _rows.RemoveAt(index);
        Rename();
    }

    public void AddColumn(int index) {
        foreach (List<Cell> row in _rows) row.Insert(index, new Cell());
        Rename();
    }

    public void RemoveColumn(int index) {
        foreach (List<Cell> row in _rows) row.RemoveAt(index);
        Rename();
    }
}