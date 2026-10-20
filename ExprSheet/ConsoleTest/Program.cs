using Core;

Sheet sheet = new Sheet();
Parser parser = new Parser();
Evaluator evaluator = new Evaluator();

void Check(string text) {
    Console.WriteLine($"  [{text}]");

    Node? tree = parser.Parse(text);
    if (tree == null) {
        string pad = new string(' ', Math.Max(0, parser.ErrorPos) + 3);
        Console.WriteLine($"{pad}^ {parser.ErrorText}  (позиція {parser.ErrorPos + 1})");
        Console.WriteLine();
        return;
    }

    Value? value = evaluator.Evaluate(tree, sheet);
    if (value == null) {
        Console.WriteLine($"   Етап 1: {tree.ToText()}");
        Console.WriteLine($"   Етап 2 — помилка: {evaluator.ErrorText}");
    } else {
        Console.WriteLine($"   OK: {tree.ToText()} = {value}");
    }
    Console.WriteLine();
}

void CheckLong(string name, string text) {
    Console.WriteLine($"  {name}");
    Node? tree = parser.Parse(text);
    if (tree == null) {
        Console.WriteLine($"   Етап 1 — помилка: {parser.ErrorText}  (позиція {parser.ErrorPos + 1})");
    } else {
        Value? v = evaluator.Evaluate(tree, sheet);
        Console.WriteLine(v == null
            ? $"   Етап 2 — помилка: {evaluator.ErrorText}"
            : $"   OK = {v}");
    }
    Console.WriteLine();
}

Check("not 5");
Check("1 / 0");
Check("2 ^ -1");
Check("2 ^ 1000000");
Check("(2 <= 3) <= 4");
Check("2 <= (3 <= 4)");

CheckLong("100 дужок",    new string('(', 100) + "1" + new string(')', 100));
CheckLong("600 дужок",    new string('(', 600) + "1" + new string(')', 600));
CheckLong("600 мінусів",  new string('-', 600) + "1");
CheckLong("100 доданків", string.Join("+", Enumerable.Repeat("1", 100)));
CheckLong("600 доданків", string.Join("+", Enumerable.Repeat("1", 600)));