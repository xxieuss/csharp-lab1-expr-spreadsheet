using System.Numerics;

namespace Core;

public class NumberNode : Node {
    private readonly BigInteger _value;

    public NumberNode(BigInteger value) {
        _value = value;
    }

    public override Value Evaluate(Sheet sheet, int depth, List<string>? trace) => Value.FromNumber(_value);

    public override string ToText() => _value.ToString();

    public override List<string> CollectRefs() => new List<string>();

    public override string ToTree(int level) => $"{Indent(level)}число {_value}\n";
}