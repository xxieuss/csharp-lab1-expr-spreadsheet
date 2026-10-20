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

Check("42");
Check("   7   ");
Check("(123)");
Check("((5))");
Check("123456789012345678901234567890");
Check("");
Check("(42");
Check("42)");
Check("4 2");
Check("12 # 3");