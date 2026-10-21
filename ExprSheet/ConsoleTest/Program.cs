using Core;

SheetController app = new SheetController();
app.NewSheet(4, 3);

string Show(Cell c) => c.HasError() ? $"ПОМИЛКА: {c.ErrorText}" : c.Value.ToString();

void Set(string name, string formula) {
    Cell cell = app.SelectCell(name);
    bool ok = app.SetCellFormula(cell, formula);
    Console.WriteLine($"{(ok ? "ok" : "!!")}  {name} = {formula,-16} -> {Show(cell)}");
}

void Dump(string title, DisplayMode mode) {
    Console.WriteLine();
    Console.WriteLine(title);
    Console.Write("    ");
    for (int c = 0; c < app.Sheet.Cols; c++) Console.Write($"{Sheet.ColumnName(c),14}");
    Console.WriteLine();
    for (int r = 0; r < app.Sheet.Rows; r++) {
        Console.Write($"{r + 1,3} ");
        for (int c = 0; c < app.Sheet.Cols; c++)
            Console.Write($"{app.Sheet.GetCell(r, c).Display(mode),14}");
        Console.WriteLine();
    }
}

Set("A1", "10");
Set("A2", "20");
Set("B1", "A1 + A2");
Set("C1", "B1 <= 100");

Dump("Режим ЗНАЧЕННЯ:", DisplayMode.Value);
Dump("Режим ВИРАЗ:", DisplayMode.Expression);

Console.WriteLine($"\nAddRow(2) -> {app.AddRow(2)}");
Dump("Рядок вставлено між 2-м і 3-м, нічого не змістилось:", DisplayMode.Value);

Console.WriteLine($"\nAddColumn(1) -> {app.AddColumn(1)}");
Dump("Стовпець вставлено між A і B:", DisplayMode.Value);
Dump("а формули ті самі:", DisplayMode.Expression);

Console.WriteLine($"\nRemoveColumn(1) -> {app.RemoveColumn(1)}");
Dump("Прибрали його назад:", DisplayMode.Value);

Console.WriteLine($"\nRemoveColumn(9) -> {app.RemoveColumn(9)}   (індекс поза межами)");
while (app.Sheet.Rows > 1) app.RemoveRow(app.Sheet.Rows - 1);
Console.WriteLine($"RemoveRow(0) при одному рядку -> {app.RemoveRow(0)}");
Dump("Лишився один рядок:", DisplayMode.Value);