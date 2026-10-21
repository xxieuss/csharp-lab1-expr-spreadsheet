using Core;

SheetController app = new SheetController();
app.NewSheet(4, 3);

string Show(Cell c) => c.HasError() ? $"ПОМИЛКА: {c.ErrorText}" : c.Value.ToString();

void Set(string name, string formula) {
    Cell cell = app.SelectCell(name);
    bool ok = app.SetCellFormula(cell, formula);
    Console.WriteLine($"{(ok ? "ok" : "!!")}  {name} = {formula,-22} -> {Show(cell)}");
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

void TryLoad(IStorage storage, string path, string what) {
    try {
        app.Load(storage, path);
        Console.WriteLine($"{what}: завантажено");
    } catch (StorageException ex) {
        Console.WriteLine($"{what}: {ex.Message}");
    }
}

Set("A1", "10");
Set("A2", "4");
Set("B1", "(A1 + A2) * 2 <= 30");
Set("C1", "if B1 then A1 ^ A2");
Set("B2", "A3 := A1 + A2");

Console.WriteLine("\nJSON:");
Console.WriteLine(app.Sheet.ToJson());

IStorage storage = new LocalFileStorage();
string good = Path.Combine(Path.GetTempPath(), "sheet.json");
app.Save(storage, good);
Console.WriteLine($"Збережено у {good}");

app.NewSheet(2, 2);
Dump("Нова порожня таблиця:", DisplayMode.Value, 14);

TryLoad(storage, good, "\nЗавантаження");
Dump("Значення:", DisplayMode.Value, 14);
Dump("Формули:", DisplayMode.Expression, 24);

Console.WriteLine();
TryLoad(storage, Path.Combine(Path.GetTempPath(), "no-such-file.json"), "Якого немає");

string bad = Path.Combine(Path.GetTempPath(), "bad.json");
File.WriteAllText(bad, "{ це не json ");
TryLoad(storage, bad, "Сміття замість JSON");

File.WriteAllText(bad, "{\"Rows\":1000000,\"Cols\":1000000,\"Cells\":[]}");
TryLoad(storage, bad, "Мільйон на мільйон");

Dump("Після трьох невдалих завантажень таблиця ціла:", DisplayMode.Value, 14);