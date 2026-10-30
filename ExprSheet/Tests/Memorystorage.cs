using Core;

namespace Tests;

internal sealed class MemoryStorage : IStorage {
    private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

    public int SaveCount { get; private set; }
    public int LoadCount { get; private set; }

    public void Save(Sheet sheet, string name) {
        _files[name] = sheet.ToJson();
        SaveCount++;
    }

    public Sheet Load(string name) {
        LoadCount++;
        if (!_files.TryGetValue(name, out string? json)) throw new StorageException($"Немає файлу «{name}»");
        return Sheet.FromJson(json);
    }
}