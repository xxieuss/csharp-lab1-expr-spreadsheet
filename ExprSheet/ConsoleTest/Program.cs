using Core;

SheetController app = new SheetController();
app.NewSheet(5, 4);

string Show(Cell c) => c.HasError() ? $"ПОМИЛКА: {c.ErrorText}" : c.Value.ToString();

void Set(string name, string formula) {
    Cell cell = app.SelectCell(name);
    bool ok = app.SetCellFormula(cell, formula);
    Console.WriteLine($"{(ok ? "ok" : "!!")}  {name} = {formula,-22} -> {Show(cell)}");
}

void ShowTree(string name) {
    Cell cell = app.SelectCell(name);
    Console.WriteLine($"\nДерево розбору {name}   [{cell.Formula}]");
    Node? tree = app.GetTree(cell);
    Console.Write(tree == null ? "   (немає)\n" : tree.ToTree(1));
}

void ShowTrace(string name) {
    Cell cell = app.SelectCell(name);
    Console.WriteLine($"\nПокрокове обчислення {name}   [{cell.Formula}]");
    foreach (string line in app.GetTrace(cell)) Console.WriteLine($"   {line}");
}

Set("A1", "10");
Set("A2", "4");
Set("B1", "(A1 + A2) * 2 <= 30");
Set("C1", "if B1 then A1 ^ A2");
Set("B2", "A4 := A1 + A2");
Set("D1", "A1 / (A2 - 4)");

ShowTree("B1");
ShowTrace("B1");
ShowTree("C1");
ShowTrace("C1");
ShowTrace("B2");
ShowTrace("D1");