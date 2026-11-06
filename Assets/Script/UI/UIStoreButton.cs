using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Buys stacks of an item into a UISpawnerSlot. Put it on a uGUI Button.
/// Greys itself out when the player can't afford it or the slot can't take it.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIStoreButton : MonoBehaviour
{
    [SerializeField] private UIItem itemPrefab;
    [SerializeField, Min(0)] private int cost = 10;
    [SerializeField, Min(1)] private int amount = 1;
    [SerializeField] private UISpawnerSlot targetSlot;
    [SerializeField] private Wallet wallet;
    // Optional. {0} = item id, {1} = cost, {2} = amount.
    [SerializeField] private TMP_Text label;
    [SerializeField] private string labelFormat = "Buy {0} x{2} - ${1}";

    public event Action<UIItem, int> Purchased;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(Purchase);

        if (wallet != null)
        {
            wallet.MoneyChanged += HandleMoneyChanged;
        }

        if (targetSlot != null)
        {
            targetSlot.StacksChanged += HandleStacksChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(Purchase);

        if (wallet != null)
        {
            wallet.MoneyChanged -= HandleMoneyChanged;
        }

        if (targetSlot != null)
        {
            targetSlot.StacksChanged -= HandleStacksChanged;
        }
    }

    public bool CanPurchase()
    {
        if (itemPrefab == null || targetSlot == null || wallet == null)
        {
            return false;
        }

        if (wallet.Money < cost)
        {
            return false;
        }

        if (!targetSlot.CanReceive(itemPrefab))
        {
            return false;
        }

        return targetSlot.CanAddStacks(amount);
    }

    /// <summary>
    /// Wired to the Button automatically. Also callable from code.
    /// </summary>
    public void Purchase()
    {
        if (!CanPurchase())
        {
            return;
        }

        // Free items: TrySpend rejects 0, so skip it.
        if (cost > 0 && !wallet.TrySpend(cost))
        {
            return;
        }

        if (!targetSlot.AddStacks(itemPrefab, amount))
        {
            // Shouldn't happen after CanPurchase; refund to be safe.
            wallet.Add(cost);
            return;
        }

        Purchased?.Invoke(itemPrefab, amount);
        Refresh();
    }

    private void HandleMoneyChanged(int money)
    {
        Refresh();
    }

    private void HandleStacksChanged(int stacks)
    {
        Refresh();
    }

    private void Refresh()
    {
        button.interactable = CanPurchase();

        if (label != null && itemPrefab != null)
        {
            label.text = string.Format(labelFormat, itemPrefab.ItemId, cost, amount);
        }
    }

    private void OnValidate()
    {
        if (itemPrefab != null && targetSlot != null && targetSlot.ItemPrefab != null)
        {
            if (targetSlot.ItemPrefab.ItemId != itemPrefab.ItemId)
            {
                Debug.LogWarning("UIStoreButton: item doesn't match the target slot's item. " +
                    "Purchases only work while that slot is empty.", this);
            }
        }
    }
}
