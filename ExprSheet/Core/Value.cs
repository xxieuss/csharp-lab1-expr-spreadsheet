using System.Numerics;

namespace Core;

public enum ValueKind
{
    Empty,
    Number,
    Boolean
}

public class Value {
    public ValueKind Kind { get; }

    private readonly BigInteger _number;
    private readonly bool _flag;

    private Value(ValueKind kind, BigInteger number, bool flag) {
        Kind = kind;
        _number = number;
        _flag = flag;
    }

    public static readonly Value Empty = new Value(ValueKind.Empty, BigInteger.Zero, false);

    public static Value FromNumber(BigInteger n) => new Value(ValueKind.Number, n, false);

    public static Value FromBool(bool b) => new Value(ValueKind.Boolean, BigInteger.Zero, b);

    public BigInteger AsNumber() {
        if (Kind != ValueKind.Number) throw new InvalidOperationException($"Очікувалося число, а отримано {Kind}");
        return _number;
    }

    public bool AsBool() {
        if (Kind != ValueKind.Boolean) throw new InvalidOperationException($"Очікувалося логічне значення, а отримано {Kind}");
        return _flag;
    }

    public override string ToString() => Kind switch {
        ValueKind.Number  => _number.ToString(),
        ValueKind.Boolean => _flag ? "True" : "False",
        _                 => ""
    };
}