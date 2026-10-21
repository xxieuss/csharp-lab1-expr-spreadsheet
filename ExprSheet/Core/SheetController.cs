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

    public void ClearCell(Cell cell) {
        cell.Clear();
        Graph.SetDependencies(cell, new List<Cell>());
        Recalculate(cell);
    }

    public bool SetCellFormula(Cell cell, string text) {
        cell.Formula = text;
        Relink(cell);
        Recalculate(cell);
        return !cell.HasError();
    }

    private bool Relink(Cell cell) {
        cell.ErrorText = "";
        cell.ErrorPos = -1;
        cell.Ast = null;
        cell.Value = Value.Empty;
        Graph.SetDependencies(cell, new List<Cell>());

        if (cell.Formula.Trim().Length == 0) return true;

        Node? ast = _parser.Parse(cell.Formula);
        if (ast == null) {
            cell.ErrorText = _parser.ErrorText;
            cell.ErrorPos = _parser.ErrorPos;
            return false;
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

        if (Graph.HasCycle(cell)) {
            Graph.SetDependencies(cell, new List<Cell>());
            cell.ErrorText = "Циклічне посилання";
            return false;
        }

        cell.Ast = ast;
        return true;
    }

    private void Recalculate(Cell start) {
        EvaluateOne(start);

        HashSet<Cell> affected = new HashSet<Cell>();
        Stack<Cell> stack = new Stack<Cell>();
        stack.Push(start);

        while (stack.Count > 0) {
            foreach (Cell d in Graph.GetDependents(stack.Pop()))
                if (affected.Add(d)) stack.Push(d);
        }

        if (affected.Count == 0) return;

        foreach (Cell c in Graph.TopologicalOrder())
            if (affected.Contains(c)) EvaluateOne(c);
    }

    private void EvaluateOne(Cell cell) {
        if (cell.Ast == null) {
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
        if (index < 0 || index > Sheet.Rows) return false;
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
        if (index < 0 || index > Sheet.Cols) return false;
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
                Relink(cell);
            }
        }
        if (!selectedAlive) Selected = null;

        foreach (Cell cell in Graph.TopologicalOrder()) EvaluateOne(cell);
    }
}