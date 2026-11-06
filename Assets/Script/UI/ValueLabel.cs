using TMPro;
using UnityEngine;

/// <summary>
/// Shows a SlotGroup's total or a Wallet's money on a TextMeshPro label.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ValueLabel : MonoBehaviour
{
    private enum Source
    {
        SlotGroupTotal,
        WalletMoney
    }

    [SerializeField] private Source source = Source.SlotGroupTotal;
    [SerializeField] private UISlotGroup slotGroup;
    [SerializeField] private Wallet wallet;
    // {0} is replaced with the number.
    [SerializeField] private string format = "Total: {0}";

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        switch (source)
        {
            case Source.SlotGroupTotal:
                if (slotGroup != null)
                {
                    slotGroup.TotalChanged += Refresh;
                    Refresh(slotGroup.TotalValue);
                }
                break;
            case Source.WalletMoney:
                if (wallet != null)
                {
                    wallet.MoneyChanged += Refresh;
                    Refresh(wallet.Money);
                }
                break;
        }
    }

    private void OnDisable()
    {
        switch (source)
        {
            case Source.SlotGroupTotal:
                if (slotGroup != null)
                {
                    slotGroup.TotalChanged -= Refresh;
                }
                break;
            case Source.WalletMoney:
                if (wallet != null)
                {
                    wallet.MoneyChanged -= Refresh;
                }
                break;
        }
    }

    private void Refresh(int amount)
    {
        label.text = string.Format(format, amount);
    }
}
