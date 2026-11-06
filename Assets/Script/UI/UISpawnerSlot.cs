using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A source slot that holds a stack of one item type.
/// Shows one copy of the item while Stacks > 0. Dragging that copy into
/// another slot uses up one stack and spawns the next copy. A failed drop
/// returns the copy and costs nothing.
/// Use AddStacks() (or a UIStoreButton) to refill it.
/// </summary>
public class UISpawnerSlot : UISlot
{
    // Prefab asset of the item this slot spawns. Can be left empty and set
    // later by a UIStoreButton purchase.
    [SerializeField] private UIItem itemPrefab;
    [SerializeField, Min(0)] private int startingStacks = 5;
    // 0 = unlimited.
    [SerializeField, Min(0)] private int maxStacks = 0;
    // Dropping a matching item back onto this slot adds it to the stack.
    [SerializeField] private bool acceptReturns = true;
    // Optional count display. {0} is the number of stacks.
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private string countFormat = "x{0}";
    // Infinite: never runs out, hides the count, returns just disappear.
    [SerializeField] private bool infiniteStacks = false;

    public UIItem ItemPrefab
    {
        get { return itemPrefab; }
    }

    public int Stacks { get; private set; }

    public bool IsInfinite
    {
        get { return infiniteStacks; }
    }

    public event Action<int> StacksChanged;
    public event Action<bool> InfiniteChanged;

    private UIItem spawnedItem;

    // Items only leave this slot; they are never swapped in.
    public override bool SwapWhenOccupied
    {
        get { return false; }
    }

    protected override void Awake()
    {
        base.Awake();
        Stacks = startingStacks;
    }

    private void OnEnable()
    {
        ItemRemoved += HandleItemRemoved;
    }

    private void OnDisable()
    {
        ItemRemoved -= HandleItemRemoved;
    }

    private void Start()
    {
        if (itemPrefab != null && string.IsNullOrEmpty(itemPrefab.ItemId))
        {
            Debug.LogWarning("UISpawnerSlot: item prefab has no Item Id; returns and store matching won't work.", this);
        }

        RefreshSpawn();
        NotifyStacksChanged();
    }

    // ---------- Public API ----------

    /// <summary>
    /// Wire to a UI Button's OnClick.
    /// </summary>
    public void ToggleInfinite()
    {
        SetInfinite(!infiniteStacks);
    }

    /// <summary>
    /// Wire to a UI Toggle's OnValueChanged (Dynamic bool).
    /// The stack count is kept and resumes when infinite is turned off.
    /// </summary>
    public void SetInfinite(bool infinite)
    {
        if (infinite == infiniteStacks)
        {
            return;
        }

        infiniteStacks = infinite;

        if (infiniteStacks)
        {
            // May need a copy if the stack was empty.
            RefreshSpawn();
        }
        else
        {
            RemoveFreeCopy();
        }

        InfiniteChanged?.Invoke(infiniteStacks);
        // Store buttons listen to this to re-check CanAddStacks.
        NotifyStacksChanged();
    }

    public bool Matches(UIItem item)
    {
        if (item == null || itemPrefab == null)
        {
            return false;
        }

        return !string.IsNullOrEmpty(item.ItemId) && item.ItemId == itemPrefab.ItemId;
    }

    public bool CanAddStacks(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        // Nothing to buy while infinite; store buttons grey out.
        if (infiniteStacks)
        {
            return false;
        }

        if (maxStacks == 0)
        {
            return true;
        }

        return Stacks + amount <= maxStacks;
    }

    /// <summary>
    /// Adds stacks of the current item. Returns false if over the max.
    /// </summary>
    public bool AddStacks(int amount)
    {
        if (itemPrefab == null || !CanAddStacks(amount))
        {
            return false;
        }

        Stacks += amount;
        RefreshSpawn();
        NotifyStacksChanged();
        return true;
    }

    /// <summary>
    /// Adds stacks of a specific item. If the slot is empty (0 stacks),
    /// it switches to that item. Fails if it holds a different item.
    /// </summary>
    public bool AddStacks(UIItem prefab, int amount)
    {
        if (prefab == null)
        {
            return false;
        }

        if (!CanReceive(prefab))
        {
            return false;
        }

        if (itemPrefab == null || Stacks == 0)
        {
            itemPrefab = prefab;
        }

        return AddStacks(amount);
    }

    /// <summary>
    /// True if AddStacks(prefab, 1) would succeed, ignoring the max.
    /// </summary>
    public bool CanReceive(UIItem prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        if (itemPrefab == null || Stacks == 0)
        {
            return true;
        }

        return Matches(prefab);
    }

    // ---------- Drops ----------

    // Nothing is ever placed here by drag; only returns are absorbed.
    public override bool AcceptsType(UIItem item)
    {
        return false;
    }

    public override void OnDrop(PointerEventData eventData)
    {
        SetHover(false);

        UIItem item = GetDraggedItem(eventData);
        if (item == null || item == spawnedItem)
        {
            // Own copy dropped back: UIItem returns it here on its own.
            return;
        }

        if (!acceptReturns || !Matches(item))
        {
            return;
        }

        if (infiniteStacks)
        {
            // Infinite: the returned item just disappears.
            item.ConsumeOnDrop();
        }
        else if (CanAddStacks(1))
        {
            item.ConsumeOnDrop();
            AddStacks(1);
        }
    }

    // ---------- Internals ----------

    private void HandleItemRemoved(UIItem item)
    {
        if (item != spawnedItem)
        {
            return;
        }

        // The shown copy was placed in another slot: use up one stack
        // (free while infinite).
        spawnedItem = null;

        if (!infiniteStacks)
        {
            Stacks = Mathf.Max(0, Stacks - 1);
            NotifyStacksChanged();
        }

        // Don't spawn while the scene is unloading.
        if (isActiveAndEnabled)
        {
            RefreshSpawn();
        }
    }

    private void RefreshSpawn()
    {
        if (spawnedItem != null || itemPrefab == null)
        {
            return;
        }

        if (!infiniteStacks && Stacks <= 0)
        {
            return;
        }

        spawnedItem = Instantiate(itemPrefab, Content);
        spawnedItem.PlaceInstant(this);
    }

    // Leaving infinite with an empty stack: the shown copy was free, remove it.
    private void RemoveFreeCopy()
    {
        if (Stacks > 0 || spawnedItem == null || spawnedItem.IsDragging)
        {
            return;
        }

        UIItem copy = spawnedItem;
        // Clear first so HandleItemRemoved ignores it.
        spawnedItem = null;
        ClearOccupant(copy);
        Destroy(copy.gameObject);
    }

    private void NotifyStacksChanged()
    {
        if (countLabel != null)
        {
            // Disable the component, not the GameObject, so a label placed
            // on the slot itself can't hide the slot.
            countLabel.enabled = !infiniteStacks;
            countLabel.text = string.Format(countFormat, Stacks);
        }

        StacksChanged?.Invoke(Stacks);
    }
}