namespace Core;

public abstract class Node {
    protected const int MaxDepth = 500;

    public abstract Value Evaluate(Sheet sheet, int depth, List<string>? trace = null);
    public abstract string ToText();
    public abstract string ToTree(int level);
    public abstract List<string> CollectRefs();

    protected static void CheckDepth(int depth) {
        if (depth > MaxDepth) throw new EvalException("Занадто глибока вкладеність виразу");
    }

    protected static string Indent(int level) => new string(' ', level * 3);

    protected Value Report(Value value, List<string>? trace) {
        trace?.Add($"{ToText()} = {value}");
        return value;
    }
}