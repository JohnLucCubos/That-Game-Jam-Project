using System;
using UnityEngine;

/// <summary>
/// A drop target for SlottableItems. Needs a Collider2D (or Collider) on the
/// same GameObject. Holds one item at a time.
/// </summary>
public class ItemSlot : MonoBehaviour
{
    // Where the item snaps to. Defaults to this transform.
    [SerializeField] private Transform snapPoint;
    // Leave empty to accept any item. Otherwise the item must have this tag.
    [SerializeField] private string acceptedTag = "";

    public SlottableItem Occupant { get; private set; }

    public event Action<SlottableItem> ItemPlaced;
    public event Action<SlottableItem> ItemRemoved;

    public Vector3 SnapPosition
    {
        get
        {
            if (snapPoint != null)
            {
                return snapPoint.position;
            }
            return transform.position;
        }
    }

    // Override in a subclass for custom rules (item type, stack limits, etc.).
    public virtual bool CanAccept(SlottableItem item)
    {
        if (item == null)
        {
            return false;
        }

        if (Occupant != null && Occupant != item)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(acceptedTag) && !item.CompareTag(acceptedTag))
        {
            return false;
        }

        return true;
    }

    internal void SetOccupant(SlottableItem item)
    {
        Occupant = item;
        ItemPlaced?.Invoke(item);
    }

    internal void ClearOccupant(SlottableItem item)
    {
        if (Occupant != item)
        {
            return;
        }

        Occupant = null;
        ItemRemoved?.Invoke(item);
    }
}
