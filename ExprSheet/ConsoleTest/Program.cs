using Core;

Sheet sheet = new Sheet();
Parser parser = new Parser();

void Check(string text) {
    Node? tree = parser.Parse(text);
    Console.WriteLine($"  [{text}]");
    if (tree == null) {
        string pad = new string(' ', Math.Max(0, parser.ErrorPos) + 3);
        Console.WriteLine($"{pad}^ {parser.ErrorText}  (позиція {parser.ErrorPos + 1})");
    } else {
        Console.WriteLine($"   OK: {tree.ToText()} = {tree.Evaluate(sheet, 0)}");
    }
    Console.WriteLine();
}

Check("2 + 3 * 4");
Check("(2 + 3) * 4");
Check("2 ^ 3 ^ 2");
Check("-2 ^ 2");
Check("2 - 3 - 4");
Check("100 / 7 / 2");
Check("- - 5");
Check("2 * (3 + (4 - 1) * 2)");
Check("2 +");
Check("* 3");
Check("(2 + 3");