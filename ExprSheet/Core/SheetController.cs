namespace Core;

public class SheetController {
    private readonly Parser _parser = new Parser();
    private readonly Evaluator _evaluator = new Evaluator();

    public Sheet Sheet { get; private set; }
    public DependencyGraph Graph { get; private set; }
    public Cell? Selected { get; private set; }
    public DisplayMode Mode { get; private set; } = DisplayMode.Value;

    public SheetController() {
        Sheet = new Sheet(10, 5);
        Graph = new DependencyGraph();
    }

    public void NewSheet(int rows, int cols) {
        Sheet = new Sheet(rows, cols);
        Graph = new DependencyGraph();
        Selected = null;
    }

    public void SetDisplayMode(DisplayMode mode) => Mode = mode;

    public Cell SelectCell(string name) {
        Cell cell = Sheet.GetCell(name);
        Selected = cell;
        return cell;
    }

    public List<Cell> GetDependencies(Cell cell) => Graph.GetPrecedents(cell);

    public List<Cell> GetDependents(Cell cell) => Graph.GetDependents(cell);

    public bool SetCellFormula(Cell cell, string text) {
        cell.Formula = text;
        Rebuild();
        return !cell.HasError();
    }

    public void ClearCell(Cell cell) => SetCellFormula(cell, "");

    private void Link(Cell cell) {
        if (cell.Formula.Trim().Length == 0) return;

        if (cell.WrittenBy != null) {
            cell.ErrorText = $"У цю клітинку пише {cell.WrittenBy.Name}";
            return;
        }

        Node? ast = _parser.Parse(cell.Formula);
        if (ast == null) {
            cell.ErrorText = _parser.ErrorText;
            cell.ErrorPos = _parser.ErrorPos;
            return;
        }

        List<Cell> refs = new List<Cell>();
        foreach (string name in ast.CollectRefs()) {
            try {
                refs.Add(Sheet.GetCell(name));
            } catch (EvalException) {
                // references outside the table
            }
        }

        Graph.SetDependencies(cell, refs);

        if (ast is AssignNode assign && !BindTarget(cell, assign.TargetName)) {
            Graph.SetDependencies(cell, new List<Cell>());
            return;
        }

        cell.Ast = ast;
    }

    private void EvaluateOne(Cell cell) {
        if (cell.Ast == null) {
            if (cell.WrittenBy != null) {
                cell.Value = cell.WrittenBy.Value;
                cell.ErrorText = cell.WrittenBy.ErrorText;
                return;
            }
            if (!cell.HasError()) cell.Value = Value.Empty;
            return;
        }

        Value? result = _evaluator.Evaluate(cell.Ast, Sheet);
        if (result == null) {
            cell.Value = Value.Empty;
            cell.ErrorText = _evaluator.ErrorText;
        } else {
            Sheet.SetValue(cell, result);
        }
    }

    public bool AddRow(int index) {
        if (Sheet.Rows >= Sheet.MaxRows || index < 0 || index > Sheet.Rows) return false;
        Sheet.AddRow(index);
        Rebuild();
        return true;
    }

    public bool RemoveRow(int index) {
        if (Sheet.Rows <= 1 || index < 0 || index >= Sheet.Rows) return false;
        Sheet.RemoveRow(index);
        Rebuild();
        return true;
    }

    public bool AddColumn(int index) {
        if (Sheet.Cols >= Sheet.MaxCols || index < 0 || index > Sheet.Cols) return false;
        Sheet.AddColumn(index);
        Rebuild();
        return true;
    }

    public bool RemoveColumn(int index) {
        if (Sheet.Cols <= 1 || index < 0 || index >= Sheet.Cols) return false;
        Sheet.RemoveColumn(index);
        Rebuild();
        return true;
    }

    private void Rebuild() {
        Graph = new DependencyGraph();

        bool selectedAlive = false;
        for (int r = 0; r < Sheet.Rows; r++) {
            for (int c = 0; c < Sheet.Cols; c++) {
                Cell cell = Sheet.GetCell(r, c);
                if (cell == Selected) selectedAlive = true;
                string formula = cell.Formula;
                cell.Clear();
                cell.Formula = formula;
                cell.WritesTo = null;
                cell.WrittenBy = null;
            }
        }
        if (!selectedAlive) Selected = null;

        for (int r = 0; r < Sheet.Rows; r++)
            for (int c = 0; c < Sheet.Cols; c++)
                Link(Sheet.GetCell(r, c));

        for (int r = 0; r < Sheet.Rows; r++) {
            for (int c = 0; c < Sheet.Cols; c++) {
                Cell cell = Sheet.GetCell(r, c);
                if (cell.Ast == null) continue;
                if (!Graph.HasCycle(cell)) continue;
                UnbindTarget(cell);
                Graph.SetDependencies(cell, new List<Cell>());
                cell.Ast = null;
                cell.ErrorText = "Циклічне посилання";
            }
        }

        foreach (Cell cell in Graph.TopologicalOrder()) EvaluateOne(cell);
    }

    private bool BindTarget(Cell cell, string targetName) {
        Cell target;
        try {
            target = Sheet.GetCell(targetName);
        } catch (EvalException ex) {
            cell.ErrorText = ex.Message;
            return false;
        }

        if (target == cell) {
            cell.ErrorText = "Клітинка не може присвоювати сама собі";
            return false;
        }
        if (target.Formula.Trim().Length > 0) {
            cell.ErrorText = $"Клітинка {target.Name} має власний вміст";
            return false;
        }
        if (target.WrittenBy != null) {
            cell.ErrorText = $"У клітинку {target.Name} вже пише {target.WrittenBy.Name}";
            return false;
        }

        cell.WritesTo = target;
        target.WrittenBy = cell;
        Graph.SetDependencies(target, new List<Cell> { cell });
        return true;
    }

    private void UnbindTarget(Cell cell) {
        if (cell.WritesTo == null) return;
        cell.WritesTo.WrittenBy = null;
        Graph.SetDependencies(cell.WritesTo, new List<Cell>());
        cell.WritesTo = null;
    }

    public Node? GetTree(Cell cell) => cell.Ast;

    public List<string> GetTrace(Cell cell) => cell.Ast == null ? new List<string>() : _evaluator.EvaluateWithTrace(cell.Ast, Sheet);

    public void Save(IStorage storage, string name) => storage.Save(Sheet, name);

    public void Load(IStorage storage, string name) {
        Sheet = storage.Load(name);
        Rebuild();
    }
}