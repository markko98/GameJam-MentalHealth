using System;

public static class ServiceProvider
{
    public static IPersistentStorageManager storage = new LocalPersistentStorageManager();

    public static Random randomizer = new Random();
}