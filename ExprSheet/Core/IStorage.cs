namespace Core;

public interface IStorage {
    void Save(Sheet sheet, string name);
    Sheet Load(string name);
}