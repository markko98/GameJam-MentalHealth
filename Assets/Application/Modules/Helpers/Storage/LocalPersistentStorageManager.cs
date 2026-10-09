using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalPersistentStorageManager : IPersistentStorageManager
{
    private static string Key(WalletAccount t) => $"{StorageKeys.CurrencyWalletSufix}.{t.ToString().ToLower()}";
    
    public void SaveWallet(WalletAccount walletAccount, Dictionary<CurrencyType, int> balance)
    {
        Serializer.SaveToPlayerPrefs(Key(walletAccount), balance);
    }

    public Dictionary<CurrencyType, int> LoadWallet(WalletAccount walletAccount, Dictionary<CurrencyType, int> defaultBalance)
    {
        return Serializer.LoadFromPlayerPrefs<Dictionary<CurrencyType, int>>(Key(walletAccount)) ?? defaultBalance;
    }

    public int LoadInt(string key, int defaultValue)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

}