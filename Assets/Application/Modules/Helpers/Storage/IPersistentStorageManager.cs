using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPersistentStorageManager
{
    void SaveWallet(WalletAccount walletAccount, Dictionary<CurrencyType, int> balance);
    Dictionary<CurrencyType, int> LoadWallet(WalletAccount walletAccount, Dictionary<CurrencyType, int> defaultBalance);

    int LoadInt(string key, int defaultValue);
    void SaveInt(string key, int value);
}