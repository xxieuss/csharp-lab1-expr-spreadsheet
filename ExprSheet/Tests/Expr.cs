using Core;
using Xunit;

namespace Tests;

internal static class Expr {
    public static Node Parse(string source) {
        Parser parser = new Parser();
        Node? node = parser.Parse(source);
        Assert.True(node != null, $"очікувався розбір, а отримано помилку: {parser.ErrorText}");
        return node!;
    }

    public static string Text(string source) => Parse(source).ToText();

    public static string ParseError(string source) {
        Parser parser = new Parser();
        Node? node = parser.Parse(source);
        Assert.True(node == null, "очікувалася синтаксична помилка, а вираз розібрався");
        return parser.ErrorText;
    }

    public static int ParseErrorPosition(string source) {
        Parser parser = new Parser();
        parser.Parse(source);
        return parser.ErrorPos;
    }

    public static Value Eval(string source, Sheet? sheet = null) {
        Evaluator evaluator = new Evaluator();
        Value? result = evaluator.Evaluate(Parse(source), sheet ?? new Sheet(5, 5));
        Assert.True(result != null, $"очікувалося значення, а отримано помилку: {evaluator.ErrorText}");
        return result!;
    }

    public static string EvalError(string source, Sheet? sheet = null) {
        Evaluator evaluator = new Evaluator();
        Value? result = evaluator.Evaluate(Parse(source), sheet ?? new Sheet(5, 5));
        Assert.True(result == null, "очікувалася помилка обчислення, а вираз обчислився");
        return evaluator.ErrorText;
    }

    public static int Number(string source, Sheet? sheet = null) => (int)Eval(source, sheet).AsNumber();

    public static bool Bool(string source, Sheet? sheet = null) => Eval(source, sheet).AsBool();
}