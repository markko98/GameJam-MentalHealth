// Swappable global accessor, same shared-instance shape as GameTicker/DelayedExecutionManager.
// Defaults to local-only storage; a cloud-backed IPersistenceStore (e.g. Firebase) can be
// swapped in later via SetStore without any caller changes.
public static class PersistenceService
{
    private static IPersistenceStore _store = new LocalPersistenceStore();

    public static IPersistenceStore Store => _store;

    public static void SetStore(IPersistenceStore store)
    {
        _store = store;
    }
}
