namespace Core;

public class AssignNode : Node {
    private readonly string _target;
    private readonly Node _expression;

    public AssignNode(string target, Node expression) {
        _target = target;
        _expression = expression;
    }

    public string TargetName => _target;

    public override Value Evaluate(Sheet sheet, int depth) {
        CheckDepth(depth);
        return _expression.Evaluate(sheet, depth + 1);
    }

    public override string ToText() => $"{_target} := {_expression.ToText()}";

    public override List<string> CollectRefs() => _expression.CollectRefs();
}