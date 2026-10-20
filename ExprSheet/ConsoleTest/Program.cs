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
        try {
            Console.WriteLine($"   OK: {tree.ToText()} = {tree.Evaluate(sheet, 0)}");
        } catch (InvalidOperationException ex) {
            Console.WriteLine($"   Етап 1 пройдено: {tree.ToText()}");
            Console.WriteLine($"   Етап 2 – помилка: {ex.Message}");
        }
    }
    Console.WriteLine();
}

Check("2 <= 3");
Check("3 <= 2");
Check("2 + 3 <= 4 * 2");
Check("not 2 <= 3");
Check("not not 2 <= 3");
Check("5 <> 5");
Check("5 <> 6");
Check("(2 <= 3) <> (4 <= 3)");
Check("2 ^ 10 >= 1000");
Check("NOT 2 <= 3");
Check("not 5");
Check("2 <= 3 <= 4");
Check("3 < 4");