using Core;

SheetController app = new SheetController();
app.NewSheet(5, 4);

string Show(Cell c) => c.HasError() ? $"ПОМИЛКА: {c.ErrorText}" : c.Value.ToString();

void Set(string name, string formula) {
    Cell cell = app.SelectCell(name);
    bool ok = app.SetCellFormula(cell, formula);
    Console.WriteLine($"{(ok ? "ok" : "!!")}  {name} = {formula,-16} -> {Show(cell)}");
}

void Dump(string title) {
    Console.WriteLine();
    Console.WriteLine(title);
    Console.Write("    ");
    for (int c = 0; c < app.Sheet.Cols; c++) Console.Write($"{Sheet.ColumnName(c),12}");
    Console.WriteLine();
    for (int r = 0; r < app.Sheet.Rows; r++) {
        Console.Write($"{r + 1,3} ");
        for (int c = 0; c < app.Sheet.Cols; c++)
            Console.Write($"{app.Sheet.GetCell(r, c).Display(DisplayMode.Value),12}");
        Console.WriteLine();
    }
    Console.WriteLine();
}

Set("A1", "10");
Set("A2", "20");
Set("B1", "A1 + A2");
Set("B2", "B1 * 2");
Set("C1", "B2 <= 100");
Dump("Режим ЗНАЧЕННЯ:");

Set("A1", "100");
Dump("Після A1 = 100 — залежні перерахувались самі:");

Set("D1", "D2 + 1");
Set("D2", "D1 + 1");
Set("D3", "D3 + 1");
Set("C2", "A1 +");
Set("C3", "A1 / 0");

Console.WriteLine();
Console.WriteLine("Залежності B1: " +
    string.Join(", ", app.GetDependencies(app.Sheet.GetCell("B1")).Select(c => c.Name)));
Console.WriteLine("Залежності B2: " +
    string.Join(", ", app.GetDependencies(app.Sheet.GetCell("B2")).Select(c => c.Name)));
Console.WriteLine("Порядок перерахунку: " +
    string.Join(" -> ", app.Graph.TopologicalOrder().Select(c => c.Name)));