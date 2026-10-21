namespace Core;

public class CellRefNode : Node {
    private readonly string _name;

    public CellRefNode(string name) {
        _name = name;
    }

    public string Name => _name;

    public override Value Evaluate(Sheet sheet, int depth) {
        CheckDepth(depth);
        return sheet.GetValue(_name);
    }

    public override string ToText() => _name;

    public override List<string> CollectRefs() => new List<string> { _name };
}