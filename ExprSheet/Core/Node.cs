namespace Core;

public abstract class Node {
    protected const int MaxDepth = 500;

    public abstract Value Evaluate(Sheet sheet, int depth);
    public abstract string ToText();
    public abstract List<string> CollectRefs();

    protected static void CheckDepth(int depth) {
        if (depth > MaxDepth) throw new InvalidOperationException("Занадто глибока вкладеність виразу");
    }
}