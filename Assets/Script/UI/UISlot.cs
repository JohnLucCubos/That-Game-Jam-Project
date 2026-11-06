using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A drop target for UIItems. Needs a raycast-target Graphic (e.g. an Image)
/// on this GameObject so it can receive drops. Holds one item at a time.
/// </summary>
public class UISlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    // Where the item is parented and centered. Defaults to this RectTransform.
    [SerializeField] private RectTransform content;
    // Leave empty to accept any item. Otherwise the item must have this tag.
    [SerializeField] private string acceptedTag = "";
    // Leave empty to accept any item. Otherwise the item's Item Id must be listed.
    [SerializeField] private List<string> acceptedItemIds = new List<string>();
    // Set by UIGridArea. Used by UIItem.PlacementRule.
    [SerializeField, HideInInspector] private bool isGridCell;
    // Dropping onto a full slot swaps the two items instead of rejecting.
    [SerializeField] private bool swapWhenOccupied = true;
    // Optional: shown while a valid item is dragged over this slot.
    [SerializeField] private GameObject hoverVisual;

    public UIItem Occupant { get; private set; }

    public event Action<UIItem> ItemPlaced;
    public event Action<UIItem> ItemRemoved;

    public RectTransform Content
    {
        get
        {
            if (content != null)
            {
                return content;
            }
            return (RectTransform)transform;
        }
    }

    public virtual bool SwapWhenOccupied
    {
        get { return swapWhenOccupied; }
    }

    public bool IsGridCell
    {
        get { return isGridCell; }
    }

    protected virtual void Awake()
    {
        if (hoverVisual == gameObject)
        {
            Debug.LogWarning("UISlot: Hover Visual can't be the slot itself (it would disable it). " +
                "Use a child object instead. Ignoring.", this);
            hoverVisual = null;
        }

        SetHover(false);
    }

    // Type rules only (ignores whether the slot is full).
    // Override in a subclass for custom rules (item category, etc.).
    public virtual bool AcceptsType(UIItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(acceptedTag) && !item.CompareTag(acceptedTag))
        {
            return false;
        }

        if (acceptedItemIds.Count > 0 && !acceptedItemIds.Contains(item.ItemId))
        {
            return false;
        }

        switch (item.Placement)
        {
            case UIItem.PlacementRule.Anywhere:
                break;
            case UIItem.PlacementRule.GridOnly:
                if (!isGridCell)
                {
                    return false;
                }
                break;
            case UIItem.PlacementRule.SlotsOnly:
                if (isGridCell)
                {
                    return false;
                }
                break;
        }

        return true;
    }

    // Called by UIGridArea when it creates or reloads cells.
    internal void ConfigureAsGridCell(List<string> itemIds)
    {
        isGridCell = true;

        // Grid-level list overrides the cell template's own list when set.
        if (itemIds != null && itemIds.Count > 0)
        {
            acceptedItemIds = new List<string>(itemIds);
        }
    }

    // Type rules AND the slot is free (or already holds this item).
    public bool CanAccept(UIItem item)
    {
        if (!AcceptsType(item))
        {
            return false;
        }

        if (Occupant != null && Occupant != item)
        {
            return false;
        }

        return true;
    }

    public virtual void OnDrop(PointerEventData eventData)
    {
        SetHover(false);

        UIItem item = GetDraggedItem(eventData);
        if (item != null)
        {
            item.TryPlace(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIItem item = GetDraggedItem(eventData);
        if (item != null && AcceptsType(item))
        {
            SetHover(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHover(false);
    }

    internal void SetOccupant(UIItem item)
    {
        Occupant = item;
        ItemPlaced?.Invoke(item);
    }

    internal void ClearOccupant(UIItem item)
    {
        if (Occupant != item)
        {
            return;
        }

        Occupant = null;
        ItemRemoved?.Invoke(item);
    }

    protected static UIItem GetDraggedItem(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return null;
        }
        return eventData.pointerDrag.GetComponent<UIItem>();
    }

    protected void SetHover(bool visible)
    {
        if (hoverVisual != null)
        {
            hoverVisual.SetActive(visible);
        }
    }
}