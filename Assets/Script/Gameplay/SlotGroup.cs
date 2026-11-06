using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A set of ItemSlots treated as one area (e.g. Inventory, Crafting).
/// Keeps a live total of the ItemValue of every item in its slots.
/// Use one on the crafting area; call Sell() from a UI Button.
/// </summary>
public class SlotGroup : MonoBehaviour
{
    [SerializeField] private List<ItemSlot> slots = new List<ItemSlot>();
    // Adds every ItemSlot found under this GameObject.
    [SerializeField] private bool collectChildSlots = true;
    // Where sold value goes. Only needed if this group can sell.
    [SerializeField] private Wallet wallet;

    public int TotalValue { get; private set; }
    public IReadOnlyList<ItemSlot> Slots
    {
        get { return slots; }
    }

    public event Action<int> TotalChanged;
    public event Action<int> Sold;

    private void Awake()
    {
        if (collectChildSlots)
        {
            foreach (ItemSlot slot in GetComponentsInChildren<ItemSlot>())
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
        foreach (ItemSlot slot in slots)
        {
            slot.ItemPlaced += HandleSlotChanged;
            slot.ItemRemoved += HandleSlotChanged;
        }
    }

    private void OnDisable()
    {
        foreach (ItemSlot slot in slots)
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

    private void HandleSlotChanged(SlottableItem item)
    {
        Recalculate();
    }

    public void Recalculate()
    {
        int total = 0;

        foreach (ItemSlot slot in slots)
        {
            if (slot == null || slot.Occupant == null)
            {
                continue;
            }

            ItemValue itemValue = slot.Occupant.GetComponent<ItemValue>();
            if (itemValue != null)
            {
                total += itemValue.Value;
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
            Debug.LogWarning("SlotGroup: no Wallet assigned, can't sell.", this);
            return;
        }

        if (TotalValue <= 0)
        {
            return;
        }

        int payout = TotalValue;

        // Collect first; clearing slots fires events that change state.
        List<SlottableItem> sold = new List<SlottableItem>();
        foreach (ItemSlot slot in slots)
        {
            if (slot != null && slot.Occupant != null)
            {
                sold.Add(slot.Occupant);
            }
        }

        foreach (SlottableItem item in sold)
        {
            item.CurrentSlot.ClearOccupant(item);
            Destroy(item.gameObject);
        }

        wallet.Add(payout);
        Recalculate();
        Sold?.Invoke(payout);
    }
}
