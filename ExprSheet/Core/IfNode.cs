namespace Core;

public class IfNode : Node {
    private readonly Node _condition;
    private readonly Node _thenPart;

    public IfNode(Node condition, Node thenPart) {
        _condition = condition;
        _thenPart = thenPart;
    }

    public override Value Evaluate(Sheet sheet, int depth, List<string>? trace) {
        CheckDepth(depth);
        Value condition = _condition.Evaluate(sheet, depth + 1, trace);

        if (condition.AsBool()) {
            trace?.Add("умова істинна, обчислюємо гілку then");
            return _thenPart.Evaluate(sheet, depth + 1, trace);
        }

        trace?.Add("умова хибна, клітинка лишається порожньою");
        return Value.Empty;
    }

    public override string ToText() => $"if {_condition.ToText()} then {_thenPart.ToText()}";

    public override List<string> CollectRefs() {
        List<string> result = _condition.CollectRefs();
        result.AddRange(_thenPart.CollectRefs());
        return result;
    }

        public override string ToTree(int level) => $"{Indent(level)}умовний оператор\n" + $"{Indent(level + 1)}умова\n" 
        + _condition.ToTree(level + 2) + $"{Indent(level + 1)}тоді\n" + _thenPart.ToTree(level + 2);
}