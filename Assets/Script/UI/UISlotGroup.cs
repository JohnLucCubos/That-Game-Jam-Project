using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A set of UISlots treated as one area (e.g. Inventory, Crafting).
/// Keeps a live total of the Value of every item in its slots.
/// Put it on the crafting panel; wire Sell() to a UI Button.
/// </summary>
public class UISlotGroup : MonoBehaviour
{
    [SerializeField] private List<UISlot> slots = new List<UISlot>();
    // Adds every UISlot found under this GameObject.
    [SerializeField] private bool collectChildSlots = true;
    // Where sold value goes. Only needed if this group can sell.
    [SerializeField] private Wallet wallet;

    public int TotalValue { get; private set; }

    public IReadOnlyList<UISlot> Slots
    {
        get { return slots; }
    }

    public event Action<int> TotalChanged;
    public event Action<int> Sold;

    private void Awake()
    {
        if (collectChildSlots)
        {
            foreach (UISlot slot in GetComponentsInChildren<UISlot>(true))
            {
                if (!slots.Contains(slot))
                {
                    slots.Add(slot);
                }
            }
        }
    }

    private void OnEnable()
    {
        foreach (UISlot slot in slots)
        {
            if (slot != null)
            {
                slot.ItemPlaced += HandleSlotChanged;
                slot.ItemRemoved += HandleSlotChanged;
            }
        }
    }

    private void OnDisable()
    {
        foreach (UISlot slot in slots)
        {
            if (slot != null)
            {
                slot.ItemPlaced -= HandleSlotChanged;
                slot.ItemRemoved -= HandleSlotChanged;
            }
        }
    }

    private void Start()
    {
        Recalculate();
    }

    private void HandleSlotChanged(UIItem item)
    {
        Recalculate();
    }

    public void Recalculate()
    {
        int total = 0;

        foreach (UISlot slot in slots)
        {
            if (slot != null && slot.Occupant != null)
            {
                total += slot.Occupant.Value;
            }
        }

        TotalValue = total;
        TotalChanged?.Invoke(TotalValue);
    }

    /// <summary>
    /// Pays out TotalValue to the wallet and destroys the items.
    /// No-arg so it can be wired to a UI Button's OnClick.
    /// </summary>
    public void Sell()
    {
        if (wallet == null)
        {
            Debug.LogWarning("UISlotGroup: no Wallet assigned, can't sell.", this);
            return;
        }

        if (TotalValue <= 0)
        {
            return;
        }

        int payout = TotalValue;

        // Collect first; clearing slots fires events that change state.
        List<UIItem> sold = new List<UIItem>();
        foreach (UISlot slot in slots)
        {
            if (slot != null && slot.Occupant != null)
            {
                sold.Add(slot.Occupant);
            }
        }

        foreach (UIItem item in sold)
        {
            item.CurrentSlot.ClearOccupant(item);
            Destroy(item.gameObject);
        }

        wallet.Add(payout);
        Recalculate();
        Sold?.Invoke(payout);
    }
}
