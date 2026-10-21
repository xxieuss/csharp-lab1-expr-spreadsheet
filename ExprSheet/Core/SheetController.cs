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
        cell.ErrorText = "";
        cell.ErrorPos = -1;
        cell.Ast = null;
        cell.Value = Value.Empty;

        if (text.Trim().Length == 0) {
            Graph.SetDependencies(cell, new List<Cell>());
            Recalculate(cell);
            return true;
        }

        Node? ast = _parser.Parse(text);
        if (ast == null) {
            cell.ErrorText = _parser.ErrorText;
            cell.ErrorPos = _parser.ErrorPos;
            Graph.SetDependencies(cell, new List<Cell>());
            Recalculate(cell);
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
            Recalculate(cell);
            return false;
        }

        cell.Ast = ast;
        Recalculate(cell);
        return !cell.HasError();
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
}