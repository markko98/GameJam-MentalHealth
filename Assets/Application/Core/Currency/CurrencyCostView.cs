using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrencyCostView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image currencyImage;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private bool shortForm = true;

    private int costValue;
    private string stringValue; // if string value exists, override costValue
    private Sprite icon;
    private bool canAfford;

    public void Setup(Sprite icon, bool canAfford, int costValue, string stringValue = null)
    {
        this.icon = icon;
        this.canAfford = canAfford;
        this.costValue = costValue;
        this.stringValue = stringValue;
        Init();
    }

    private void Init()
    {
        currencyImage.sprite = icon;
        UpdateView(canAfford, costValue, stringValue);
    }

    public void UpdateView(bool canAffordOv, int costValueOv, string stringValueOv = null)
    {
        if (String.IsNullOrEmpty(stringValueOv))
        {
            currencyText.SetText(CurrencyService.GetFormatted(costValueOv, shortForm));
        }
        else
        {
            currencyText.SetText(stringValueOv);
        }
    }
    
    public void Cleanup()
    {
        if (root != null)
            GameObject.Destroy(root.gameObject);
    }
}
