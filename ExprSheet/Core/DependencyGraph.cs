namespace Core;

public class DependencyGraph {
    private readonly Dictionary<Cell, List<Cell>> _precedents = new Dictionary<Cell, List<Cell>>();
    private readonly Dictionary<Cell, List<Cell>> _dependents = new Dictionary<Cell, List<Cell>>();

    public void SetDependencies(Cell cell, List<Cell> refs) {
        if (_precedents.TryGetValue(cell, out List<Cell>? old)) {
            foreach (Cell p in old)
                if (_dependents.TryGetValue(p, out List<Cell>? list)) list.Remove(cell);
        }

        List<Cell> unique = new List<Cell>();
        foreach (Cell p in refs)
            if (!unique.Contains(p)) unique.Add(p);

        _precedents[cell] = unique;

        foreach (Cell p in unique) {
            if (!_dependents.TryGetValue(p, out List<Cell>? list)) {
                list = new List<Cell>();
                _dependents[p] = list;
            }
            if (!list.Contains(cell)) list.Add(cell);
        }
    }

    public List<Cell> GetPrecedents(Cell cell) =>
        _precedents.TryGetValue(cell, out List<Cell>? list) ? new List<Cell>(list) : new List<Cell>();

    public List<Cell> GetDependents(Cell cell) =>
        _dependents.TryGetValue(cell, out List<Cell>? list) ? new List<Cell>(list) : new List<Cell>();

    public bool HasCycle(Cell cell) {
        HashSet<Cell> seen = new HashSet<Cell>();
        Stack<Cell> stack = new Stack<Cell>();
        stack.Push(cell);

        while (stack.Count > 0) {
            Cell current = stack.Pop();
            if (!_precedents.TryGetValue(current, out List<Cell>? refs)) continue;

            foreach (Cell p in refs) {
                if (p == cell) return true;
                if (seen.Add(p)) stack.Push(p);
            }
        }
        return false;
    }

    public List<Cell> TopologicalOrder() {
        Dictionary<Cell, int> indegree = new Dictionary<Cell, int>();
        foreach (Cell c in _precedents.Keys) indegree[c] = 0;

        foreach (Cell c in _precedents.Keys)
            foreach (Cell p in _precedents[c])
                if (indegree.ContainsKey(p)) indegree[c]++;

        Queue<Cell> ready = new Queue<Cell>();
        foreach (KeyValuePair<Cell, int> pair in indegree)
            if (pair.Value == 0) ready.Enqueue(pair.Key);

        List<Cell> order = new List<Cell>();
        while (ready.Count > 0) {
            Cell current = ready.Dequeue();
            order.Add(current);

            foreach (Cell d in GetDependents(current)) {
                if (!indegree.ContainsKey(d)) continue;
                indegree[d]--;
                if (indegree[d] == 0) ready.Enqueue(d);
            }
        }
        return order;
    }
}