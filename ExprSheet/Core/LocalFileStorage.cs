namespace Core;

public class LocalFileStorage : IStorage {
    public void Save(Sheet sheet, string name) {
        try {
            File.WriteAllText(name, sheet.ToJson());
        } catch (UnauthorizedAccessException) {
            throw new StorageException("Немає прав на запис у цей файл");
        } catch (IOException ex) {
            throw new StorageException($"Не вдалося записати файл: {ex.Message}");
        }
    }

    public Sheet Load(string name) {
        string json;
        try {
            json = File.ReadAllText(name);
        } catch (FileNotFoundException) {
            throw new StorageException("Файл не знайдено");
        } catch (DirectoryNotFoundException) {
            throw new StorageException("Теку не знайдено");
        } catch (UnauthorizedAccessException) {
            throw new StorageException("Немає прав на читання цього файлу");
        } catch (IOException ex) {
            throw new StorageException($"Не вдалося прочитати файл: {ex.Message}");
        }

        return Sheet.FromJson(json);
    }
}