using Core;
using Xunit;

namespace Tests;

public class LocalFileStorageTests : IDisposable {
    private readonly string _folder;
    private readonly LocalFileStorage _storage = new LocalFileStorage();

    public LocalFileStorageTests() {
        _folder = Path.Combine(Path.GetTempPath(), "exprsheet-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    public void Dispose() {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    private string PathTo(string name) => Path.Combine(_folder, name);

    private static SheetController ControllerWith(params (string name, string formula)[] cells) {
        SheetController controller = new SheetController();
        controller.NewSheet(4, 4);
        foreach ((string name, string formula) in cells)
            controller.SetCellFormula(controller.Sheet.GetCell(name), formula);
        return controller;
    }

    [Fact]
    public void SaveCreatesTheFile() {
        string path = PathTo("таблиця.json");
        _storage.Save(ControllerWith(("A1", "1")).Sheet, path);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void SavedSheetIsLoadedBack() {
        string path = PathTo("таблиця.json");
        _storage.Save(ControllerWith(("A1", "2 + 2"), ("B1", "A1 * 3")).Sheet, path);

        Sheet loaded = _storage.Load(path);

        Assert.Equal(4, loaded.Rows);
        Assert.Equal("2 + 2", loaded.GetCell("A1").Formula);
        Assert.Equal("A1 * 3", loaded.GetCell("B1").Formula);
    }

    [Fact]
    public void ValuesAreNotStoredOnlyFormulas() {
        string path = PathTo("таблиця.json");
        _storage.Save(ControllerWith(("A1", "2 + 2")).Sheet, path);

        string json = File.ReadAllText(path);

        Assert.Contains("Formula", json);
        Assert.Contains("2 + 2", json);
        Assert.DoesNotContain("Value", json);
    }

    [Fact]
    public void SavingTwiceOverwritesTheFile() {
        string path = PathTo("таблиця.json");
        _storage.Save(ControllerWith(("A1", "1")).Sheet, path);
        _storage.Save(ControllerWith(("A1", "2")).Sheet, path);

        Assert.Equal("2", _storage.Load(path).GetCell("A1").Formula);
    }

    [Fact]
    public void MissingFileIsReported() {
        StorageException error = Assert.Throws<StorageException>(() => _storage.Load(PathTo("немає.json")));
        Assert.Contains("Файл не знайдено", error.Message);
    }

    [Fact]
    public void MissingFolderIsReported() {
        string path = Path.Combine(_folder, "немає-теки", "таблиця.json");
        StorageException error = Assert.Throws<StorageException>(() => _storage.Load(path));
        Assert.Contains("Теку не знайдено", error.Message);
    }

    [Fact]
    public void BrokenFileIsReported() {
        string path = PathTo("зламаний.json");
        File.WriteAllText(path, "це взагалі не json");

        StorageException error = Assert.Throws<StorageException>(() => _storage.Load(path));
        Assert.Contains("коректним JSON", error.Message);
    }

    [Fact]
    public void SavingIntoAMissingFolderIsReported() {
        string path = Path.Combine(_folder, "немає-теки", "таблиця.json");
        StorageException error = Assert.Throws<StorageException>(() => _storage.Save(ControllerWith(("A1", "1")).Sheet, path));
        Assert.Contains("Не вдалося записати", error.Message);
    }

    [Fact]
    public void ControllerWorksWithTheSameStorageThroughTheInterface() {
        string path = PathTo("таблиця.json");
        IStorage storage = _storage;

        SheetController source = ControllerWith(("A1", "2 + 2"), ("B1", "A1 * 3"));
        source.Save(storage, path);

        SheetController target = new SheetController();
        target.Load(storage, path);

        Assert.Equal(12, (int)target.Sheet.GetCell("B1").Value.AsNumber());
    }
}