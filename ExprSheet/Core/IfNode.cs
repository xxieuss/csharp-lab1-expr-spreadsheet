namespace Core;

public class IfNode : Node {
    private readonly Node _condition;
    private readonly Node _thenPart;

    public IfNode(Node condition, Node thenPart) {
        _condition = condition;
        _thenPart = thenPart;
    }

    public override Value Evaluate(Sheet sheet, int depth) {
        CheckDepth(depth);
        if (_condition.Evaluate(sheet, depth + 1).AsBool()) return _thenPart.Evaluate(sheet, depth + 1);
        return Value.Empty;
    }

    public override string ToText() => $"if {_condition.ToText()} then {_thenPart.ToText()}";

    public override List<string> CollectRefs() {
        List<string> result = _condition.CollectRefs();
        result.AddRange(_thenPart.CollectRefs());
        return result;
    }
}