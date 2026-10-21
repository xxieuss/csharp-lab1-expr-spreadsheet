using Core;

SheetController app = new SheetController();
app.NewSheet(6, 4);

string Show(Cell c) => c.HasError() ? $"ПОМИЛКА: {c.ErrorText}" : c.Value.ToString();

void Set(string name, string formula) {
    Cell cell = app.SelectCell(name);
    bool ok = app.SetCellFormula(cell, formula);
    Console.WriteLine($"{(ok ? "ok" : "!!")}  {name} = {formula,-18} -> {Show(cell)}");
}

void Dump(string title, DisplayMode mode, int w) {
    Console.WriteLine();
    Console.WriteLine(title);
    Console.Write("    ");
    for (int c = 0; c < app.Sheet.Cols; c++)
        Console.Write(Sheet.ColumnName(c).PadLeft(w));
    Console.WriteLine();
    for (int r = 0; r < app.Sheet.Rows; r++) {
        Console.Write($"{r + 1,3} ");
        for (int c = 0; c < app.Sheet.Cols; c++)
            Console.Write(app.Sheet.GetCell(r, c).Display(mode).PadLeft(w));
        Console.WriteLine();
    }
}

Set("A1", "10");
Set("B1", "if A1 <= 10 then A1 * 2");
Set("B2", "if A1 <= 5 then A1 * 2");
Set("C1", "A3 := A1 + 5");
Set("D1", "A3 * 100");

Dump("Режим ЗНАЧЕННЯ:", DisplayMode.Value, 14);
Dump("Режим ВИРАЗ:", DisplayMode.Expression, 26);

Console.WriteLine();
Set("A1", "3");
Dump("Після A1 = 3:", DisplayMode.Value, 14);

Console.WriteLine();
Set("C2", "A3 := 1");
Set("C3", "A1 := 1");
Set("C4", "A4 := A4 + 1");
Set("C5", "5 := 1");
Set("C6", "if A1 then 1");
Set("D2", "if A1 <= 5 1");