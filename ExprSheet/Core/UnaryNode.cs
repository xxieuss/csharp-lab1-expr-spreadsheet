using System.Numerics;

namespace Core;

public class UnaryNode : Node {
    private readonly string _op;
    private readonly Node _operand;

    public UnaryNode(string op, Node operand) {
        _op = op;
        _operand = operand;
    }

    public override Value Evaluate(Sheet sheet, int depth, List<string>? trace) {
        CheckDepth(depth);
        Value v = _operand.Evaluate(sheet, depth + 1, trace);

        if (_op == "not") return Report(Value.FromBool(!v.AsBool()), trace);

        BigInteger n = v.AsNumber();
        return Report(Value.FromNumber(_op == "-" ? -n : n), trace);
    }

    public override string ToText() => _op == "not" ? $"(not {_operand.ToText()})" : $"({_op}{_operand.ToText()})";

    public override List<string> CollectRefs() => _operand.CollectRefs();

    public override string ToTree(int level) => $"{Indent(level)}операція {_op}\n" + _operand.ToTree(level + 1);
}