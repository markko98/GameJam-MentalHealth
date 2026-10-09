using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Wallet
{
    public event Action<WalletAccount, CurrencyType, int, int, CurrencyChange> OnBalanceChanged;

    private readonly Dictionary<(WalletAccount, CurrencyType), int> balances = new();

    public int GetBalance(WalletAccount account, CurrencyType type)
        => balances.GetValueOrDefault((account, type), 0);
    
    public Dictionary<CurrencyType, int> GetAllBalances(WalletAccount account)
    {
        var result = new Dictionary<CurrencyType, int>();

        foreach (var kvp in balances)
        {
            if (kvp.Key.Item1 == account)
            {
                result[kvp.Key.Item2] = kvp.Value;
            }
        }

        return result;
    }
    
    public bool CanAfford(WalletAccount account, CurrencyType type, int cost)
        => GetBalance(account, type) >= cost;

    public bool ApplyBalanceChange(in CurrencyChange change)
    {
        var key = (change.account, change.type);
        var before = balances.GetValueOrDefault(key, 0);
        var after = before + change.amount;
        if (after < 0)
        {
            Debug.LogError(
                $"[Wallet] Rejected {change.account}/{change.type} change of {change.amount} " +
                $"(balance {before}, reason '{change.reason}', source {change.source}): insufficient funds.");
            return false;
        }

        balances[key] = after;
        OnBalanceChanged?.Invoke(change.account, change.type, before, after, change);
        return true;
    }
    
    public void SetInitialBalance(WalletAccount account, CurrencyType type, int value)
        => balances[(account, type)] = value;
    
    // set initial balance without firing events
    public void SetInitialBalances(WalletAccount account, Dictionary<CurrencyType, int> balance)
    {
        foreach (var b in balance)
        {
            balances[(account, b.Key)] = b.Value;
        }
    }

    public int TransferAll(WalletAccount from, WalletAccount to)
    {
        int totalTransferred = 0;

        var keys = new List<(WalletAccount, CurrencyType)>();
        foreach (var kvp in balances)
            if (kvp.Key.Item1.Equals(from))
                keys.Add(kvp.Key);

        foreach (var key in keys)
        {
            var amount = balances[key];
            if (amount == 0) continue;

            // Fire event for subtraction. If it is refused the money never left, so don't credit it.
            if (!ApplyBalanceChange(new CurrencyChange(key.Item2, -amount, CurrencySource.Transfer, "transfer_out", from)))
                continue;
            // Fire event for addition
            ApplyBalanceChange(new CurrencyChange(key.Item2, +amount, CurrencySource.Transfer, "transfer_in", to));

            totalTransferred += amount;
        }

        return totalTransferred;
    }

    public int TransferCurrency(WalletAccount from, WalletAccount to, CurrencyType type)
    {
        var amt = GetBalance(from, type);
        if (amt == 0) return 0;

        if (!ApplyBalanceChange(new CurrencyChange(type, -amt, CurrencySource.Transfer, "transfer_out", from)))
            return 0;
        ApplyBalanceChange(new CurrencyChange(type, +amt, CurrencySource.Transfer, "transfer_in", to));

        return amt;
    }
}
