// Lazy-loaded, save-on-demand access to SettingsData. A Settings screen module
// reads SettingsService.Current, mutates it, and calls Save().
public static class SettingsService
{
    private const string Key = "settings_data";
    private static SettingsData _current;

    public static SettingsData Current => _current ??= PersistenceService.Store.Load(Key, new SettingsData());

    public static void Save()
    {
        PersistenceService.Store.Save(Key, Current);
    }
}
