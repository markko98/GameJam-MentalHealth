using System;
using System.Collections.Generic;
using System.Globalization;

// Static façade over the wallet, matching the rest of Core's services (TaskService,
// AudioService, ...) — reachable from anywhere without a locator. Balance changes are
// broadcast through UEventBus<CurrencyChangedEvent> rather than a plain C# event, so any
// listener (UI, analytics, tasks) can subscribe without holding a reference to
// CurrencyService itself. The Main account is loaded from and saved back to
// PersistenceService on every change; Session is a per-run balance and is never persisted.
public static class CurrencyService
{
    private const string SaveKey = "currency_wallet_main";

    // config
    private static readonly Dictionary<CurrencyType, int> startingBalance = new Dictionary<CurrencyType, int>
    {
        { CurrencyType.Coins, 200 },
        { CurrencyType.PremiumCoins, 10 },
    };
    private static bool useShortForm = true;
    private static IFormatProvider longFormCulture = CultureInfo.InvariantCulture;

    private static readonly Wallet wallet = new Wallet();

    static CurrencyService()
    {
        wallet.SetInitialBalances(WalletAccount.Main, LoadMainBalances());
        wallet.OnBalanceChanged += HandleBalanceChanged;
    }

    [Serializable]
    private struct BalanceEntry
    {
        public CurrencyType Type;
        public int Amount;
    }

    [Serializable]
    private class WalletSaveData
    {
        public List<BalanceEntry> Balances = new List<BalanceEntry>();
    }

    // Defaults are the fallback for any currency the save data doesn't have an entry for
    // yet (a fresh save, or a currency added to the game after players already have saves).
    private static Dictionary<CurrencyType, int> LoadMainBalances()
    {
        var balances = new Dictionary<CurrencyType, int>(startingBalance);

        var saved = PersistenceService.Store.Load<WalletSaveData>(SaveKey);
        if (saved != null)
        {
            foreach (var entry in saved.Balances)
                balances[entry.Type] = entry.Amount;
        }

        return balances;
    }

    private static void SaveMainBalances()
    {
        var data = new WalletSaveData();
        foreach (var balance in wallet.GetAllBalances(WalletAccount.Main))
            data.Balances.Add(new BalanceEntry { Type = balance.Key, Amount = balance.Value });

        PersistenceService.Store.Save(SaveKey, data);
    }

    public static void EmitSnapshot(WalletAccount account, CurrencyType type, string reason = "snapshot")
    {
        int value = wallet.GetBalance(account, type);
        RaiseChanged(account, type, value, value, 0, CurrencySource.Debug, reason);
    }

    public static void ResetCurrencyToZero(WalletAccount account, CurrencyType type, bool notify = true, string reason = "reset_to_zero")
    {
        int before = wallet.GetBalance(account, type);
        wallet.SetInitialBalance(account, type, 0);

        if (account == WalletAccount.Main) SaveMainBalances();
        if (!notify) return;

        RaiseChanged(account, type, before, 0, -before, CurrencySource.Debug, reason);
    }

    public static void ResetAccountToZero(WalletAccount account, bool notify = true, string reason = "reset_to_zero")
    {
        var allBalances = wallet.GetAllBalances(account);
        foreach (var balance in allBalances)
        {
            ResetCurrencyToZero(account, balance.Key, notify, reason);
        }
    }

    public static int GetBalance(WalletAccount account, CurrencyType type)
        => wallet.GetBalance(account, type);

    public static Dictionary<CurrencyType, int> GetAllBalance(WalletAccount account)
        => wallet.GetAllBalances(account);

    public static string GetFormatted(WalletAccount account, CurrencyType type, bool shortForm = true)
        => Format(wallet.GetBalance(account, type), shortForm);

    public static string GetFormatted(int value, bool shortForm = true)
        => Format(value, shortForm);

    public static bool TrySpend(WalletAccount account, CurrencyType type, int cost, string reason)
    {
        if (cost <= 0) return true;
        if (!wallet.CanAfford(account, type, cost)) return false;

        return wallet.ApplyBalanceChange(new CurrencyChange(
            type: type,
            amount: -cost,
            source: CurrencySource.Spend,
            reason: reason,
            account: account
        ));
    }
    public static bool CanAfford(WalletAccount account, CurrencyType type, int cost)
    {
        return cost <= 0 || wallet.CanAfford(account, type, cost);
    }

    public static void Grant(WalletAccount account, CurrencyType type, int amount, CurrencySource source, string reason)
    {
        if (amount <= 0) return;

        wallet.ApplyBalanceChange(new CurrencyChange(
            type: type,
            amount: amount,
            source: source,
            reason: reason,
            account: account
        ));
    }
    public static int TransferAll(WalletAccount from, WalletAccount to) => wallet.TransferAll(from, to);
    public static int TransferCurrency(WalletAccount from, WalletAccount to, CurrencyType type) => wallet.TransferCurrency(from, to, type);

    // event bridge
    private static void HandleBalanceChanged(
        WalletAccount account,
        CurrencyType currency,
        int before,
        int after,
        CurrencyChange change)
    {
        // Persist only MAIN account changes
        if (account == WalletAccount.Main)
        {
            SaveMainBalances();
        }

        RaiseChanged(account, currency, before, after, change.amount, change.source, change.reason);
    }

    private static void RaiseChanged(
        WalletAccount account,
        CurrencyType type,
        int before,
        int after,
        int delta,
        CurrencySource source,
        string reason)
    {
        UEventBus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(
            account: account,
            type: type,
            before: before,
            after: after,
            delta: delta,
            source: source,
            reason: reason,
            beforeFormatted: Format(before, useShortForm),
            afterFormatted: Format(after, useShortForm)
        ));
    }

    // Formatting
    private static string Format(int value, bool shortForm)
        => shortForm
            ? NumberFormatter.ShortFormat(value)
            : NumberFormatter.LongFormat(value, longFormCulture);

    public static void SetShortForm(bool enabled) => useShortForm = enabled;
    public static void SetLongFormCulture(IFormatProvider culture) => longFormCulture = culture ?? CultureInfo.InvariantCulture;
}
