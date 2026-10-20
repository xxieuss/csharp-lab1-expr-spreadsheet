using System.Numerics;

namespace Core;

public class UnaryNode : Node {
    private readonly string _op;
    private readonly Node _operand;

    public UnaryNode(string op, Node operand) {
        _op = op;
        _operand = operand;
    }

    public override Value Evaluate(Sheet sheet, int depth) {
        CheckDepth(depth);
        BigInteger v = _operand.Evaluate(sheet, depth + 1).AsNumber();
        return Value.FromNumber(_op == "-" ? -v : v);
    }

    public override string ToText() => $"({_op}{_operand.ToText()})";

    public override List<string> CollectRefs() => _operand.CollectRefs();
}