public interface IPersistenceStore
{
    void Save<T>(string key, T value);
    T Load<T>(string key, T defaultValue = default);
    bool HasKey(string key);
    void Delete(string key);
    void DeleteAll();
}
