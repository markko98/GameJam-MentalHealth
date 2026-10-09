public enum CurrencyType
{
    Coins,
    PremiumCoins
}

public enum CurrencySource
{
    Pickup,
    Reward,
    IAP,
    Admin,
    Transfer,
    Debug,
    Spend,
    Gameplay
}

public enum WalletAccount
{
    Main, // the real / persistent balance
    Session // per run balance
}

public readonly struct CurrencyChange
{
    public readonly CurrencyType type;
    public readonly int amount;
    public readonly CurrencySource source;
    public readonly string reason;
    public readonly WalletAccount account;

    public CurrencyChange(CurrencyType type, int amount, CurrencySource source, string reason, WalletAccount account = WalletAccount.Main)
    {
        this.type = type;
        this.amount = amount;
        this.source = source;
        this.reason = reason;
        this.account = account;
    }
}

