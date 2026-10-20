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
        BigInteger a = _left.Evaluate(sheet, depth + 1).AsNumber();
        BigInteger b = _right.Evaluate(sheet, depth + 1).AsNumber();

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
            default:
                throw new InvalidOperationException($"Невідома операція: {_op}");
        }
    }

    public override string ToText() => $"({_left.ToText()} {_op} {_right.ToText()})";

    public override List<string> CollectRefs() {
        List<string> result = _left.CollectRefs();
        result.AddRange(_right.CollectRefs());
        return result;
    }
}