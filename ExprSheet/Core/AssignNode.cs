namespace Core;

public class AssignNode : Node {
    private readonly string _target;
    private readonly Node _expression;

    public AssignNode(string target, Node expression) {
        _target = target;
        _expression = expression;
    }

    public string TargetName => _target;

    public override Value Evaluate(Sheet sheet, int depth, List<string>? trace) {
        CheckDepth(depth);
        Value value = _expression.Evaluate(sheet, depth + 1, trace);
        trace?.Add($"{_target} отримує {value}");
        return value;
    }

    public override string ToText() => $"{_target} := {_expression.ToText()}";

    public override List<string> CollectRefs() => _expression.CollectRefs();

    public override string ToTree(int level) => $"{Indent(level)}присвоєння в {_target}\n" + _expression.ToTree(level + 1);
}