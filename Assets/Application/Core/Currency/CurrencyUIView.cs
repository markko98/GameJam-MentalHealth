using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyUIView : MonoBehaviour
{
    [SerializeField] private WalletAccount walletAccount;
    [SerializeField] private CurrencyType currencyType = CurrencyType.Coins;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private bool shortForm = true;

    private Color defaultTextColor;
    private bool isInitialized;
    private EventBinding<CurrencyChangedEvent> balanceChangedBinding;

    private void Start()
    {
        Init();
    }

    public void Setup(WalletAccount walletAccount, CurrencyType currencyType)
    {
        this.walletAccount = walletAccount;
        this.currencyType = currencyType;
        Init();
    }

    private void Init()
    {
        if (isInitialized) return;
        isInitialized = true;
        defaultTextColor = currencyText.color;

        balanceChangedBinding = new EventBinding<CurrencyChangedEvent>(BalanceChanged);
        UEventBus<CurrencyChangedEvent>.Register(balanceChangedBinding);

        currencyText.SetText(CurrencyService.GetFormatted(walletAccount, currencyType, shortForm));
        // currencyIcon.sprite = CurrencyDataProvider.GetCurrencyIcon(currencyType);
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    public void Cleanup()
    {
        if (balanceChangedBinding == null) return;

        UEventBus<CurrencyChangedEvent>.Deregister(balanceChangedBinding);
        balanceChangedBinding = null;
    }

    private void BalanceChanged(CurrencyChangedEvent data)
    {
        if (data.account != walletAccount) return;
        if (data.type != currencyType) return;

        // update text using the passed valueAfter
        currencyText.SetText(data.afterFormatted);

        // icon popup
        currencyIcon.transform.DOKill(true);
        currencyIcon.transform.localScale = Vector3.one;
        currencyIcon.transform
            .DOPunchScale(Vector3.one * 0.3f, 0.3f, vibrato: 8, elasticity: 0.8f);

        // detect gain or spend
        bool gained = data.after > data.before;

        // flash color
        Color flashColor = gained ? Color.yellow : Color.red;

        currencyText.DOColor(flashColor, 0.15f)
            .OnComplete(() => { currencyText.DOColor(defaultTextColor, 0.15f); });
    }

}
