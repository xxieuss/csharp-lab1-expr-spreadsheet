using Core;

Sheet sheet = new Sheet(5, 4);
Parser parser = new Parser();
Evaluator evaluator = new Evaluator();

Console.WriteLine($"Таблиця {sheet.Rows}×{sheet.Cols}, стовпці: " +
    string.Join(" ", Enumerable.Range(0, sheet.Cols).Select(Sheet.ColumnName)));
Console.WriteLine();

sheet.SetValue(sheet.GetCell("A1"), Value.FromNumber(10));
sheet.SetValue(sheet.GetCell("B1"), Value.FromNumber(32));
sheet.SetValue(sheet.GetCell("a2"), Value.FromBool(true));

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

Check("A1");
Check("A1 + B1");
Check("A1 + B1 <= 42");
Check("not A2");
Check("A1 ^ 3 >= B1 * 30");
Check("C1 + 1");
Check("A99 + 1");
Check("Z1");
Check("abc");
Check("A1B2");
Check("AAAAA1");