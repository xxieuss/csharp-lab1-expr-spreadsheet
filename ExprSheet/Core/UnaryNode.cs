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
        Value v = _operand.Evaluate(sheet, depth + 1);

        if (_op == "not") return Value.FromBool(!v.AsBool());

        BigInteger n = v.AsNumber();
        return Value.FromNumber(_op == "-" ? -n : n);
    }

    public override string ToText() => _op == "not" ? $"(not {_operand.ToText()})" : $"({_op}{_operand.ToText()})";

    public override List<string> CollectRefs() => _operand.CollectRefs();
}