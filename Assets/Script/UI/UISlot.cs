using System;
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

    protected virtual void Awake()
    {
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

        return true;
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
