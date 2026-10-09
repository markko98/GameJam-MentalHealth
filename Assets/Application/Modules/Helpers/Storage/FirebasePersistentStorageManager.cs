using System;
using System.Collections.Generic;
using UnityEngine;

public class FirebasePersistentStorageManager : IPersistentStorageManager
{
    public void SaveWallet(WalletAccount walletAccount, Dictionary<CurrencyType, int> balance)
    {
        throw new NotImplementedException();
    }

    public Dictionary<CurrencyType, int> LoadWallet(WalletAccount walletAccount, Dictionary<CurrencyType, int> defaultBalance)
    {
        throw new NotImplementedException();
    }

    public int LoadInt(string key, int defaultValue)
    {
        throw new NotImplementedException();
    }

    public void SaveInt(string key, int value)
    {
        throw new NotImplementedException();
    }
}