using System.Numerics;

namespace Core;

public class BinaryNode : Node {
    private readonly string _op;
    private readonly Node _left;
    private readonly Node _right;

    public BinaryNode(string op, Node left, Node right) {
        _op = op;
        _left = left;
        _right = right;
    }

        public override Value Evaluate(Sheet sheet, int depth) {
        CheckDepth(depth);
        Value l = _left.Evaluate(sheet, depth + 1);
        Value r = _right.Evaluate(sheet, depth + 1);

        if (_op == "<>") return Value.FromBool(!AreEqual(l, r));

        BigInteger a = l.AsNumber();
        BigInteger b = r.AsNumber();

        switch (_op) {
            case "+": return Value.FromNumber(a + b);
            case "-": return Value.FromNumber(a - b);
            case "*": return Value.FromNumber(a * b);
            case "/":
                if (b == 0) throw new InvalidOperationException("Ділення на нуль");
                return Value.FromNumber(a / b);
            case "^":
                if (b < 0) throw new InvalidOperationException("Відʼємний показник степеня дає не ціле число");
                if (b > 100000) throw new InvalidOperationException("Завеликий показник степеня");
                return Value.FromNumber(BigInteger.Pow(a, (int)b));
            case "<=": return Value.FromBool(a <= b);
            case ">=": return Value.FromBool(a >= b);
            default:
                throw new InvalidOperationException($"Невідома операція: {_op}");
        }
    }

    private static bool AreEqual(Value x, Value y) {
        if (x.Kind != y.Kind) throw new InvalidOperationException("Порівнюються значення різних типів");
        if (x.Kind == ValueKind.Number) return x.AsNumber() == y.AsNumber();
        if (x.Kind == ValueKind.Boolean) return x.AsBool() == y.AsBool();
        throw new InvalidOperationException("Порожня клітинка у порівнянні");
    }

    public override string ToText() => $"({_left.ToText()} {_op} {_right.ToText()})";

    public override List<string> CollectRefs() {
        List<string> result = _left.CollectRefs();
        result.AddRange(_right.CollectRefs());
        return result;
    }
}