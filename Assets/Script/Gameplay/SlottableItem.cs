using System.Collections;
using UnityEngine;

/// <summary>
/// Marks a draggable object as slot-aware. On drop it snaps into a valid
/// ItemSlot, otherwise it returns to where it was when the drag started.
/// </summary>
public class SlottableItem : MonoBehaviour
{
    // 0 = snap instantly.
    [SerializeField] private float moveDuration = 0.12f;
    // Optional: slot this item starts in.
    [SerializeField] private ItemSlot startingSlot;

    public ItemSlot CurrentSlot { get; private set; }

    private Vector3 restPosition;
    private Coroutine moveRoutine;
    private Rigidbody2D body2D;

    private void Awake()
    {
        body2D = GetComponent<Rigidbody2D>();
        restPosition = transform.position;
    }

    private void Start()
    {
        if (startingSlot != null)
        {
            Place(startingSlot, true);
        }
    }

    public void BeginDrag()
    {
        StopMove();
        restPosition = transform.position;
    }

    /// <summary>
    /// Called on drop. Pass null when no slot was under the pointer.
    /// </summary>
    public bool TryPlace(ItemSlot slot)
    {
        if (slot == null || !slot.CanAccept(this))
        {
            MoveTo(restPosition, false);
            return false;
        }

        Place(slot, false);
        return true;
    }

    private void Place(ItemSlot slot, bool instant)
    {
        if (CurrentSlot != null && CurrentSlot != slot)
        {
            CurrentSlot.ClearOccupant(this);
        }

        CurrentSlot = slot;
        slot.SetOccupant(this);
        restPosition = slot.SnapPosition;
        MoveTo(restPosition, instant);
    }

    private void MoveTo(Vector3 target, bool instant)
    {
        StopMove();

        if (instant || moveDuration <= 0f)
        {
            SetPosition(target);
            return;
        }

        moveRoutine = StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            SetPosition(Vector3.Lerp(start, target, t));
            yield return null;
        }

        SetPosition(target);
        moveRoutine = null;
    }

    private void StopMove()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    private void SetPosition(Vector3 position)
    {
        if (body2D != null)
        {
            body2D.position = position;
        }
        transform.position = position;
    }

    private void OnDestroy()
    {
        if (CurrentSlot != null)
        {
            CurrentSlot.ClearOccupant(this);
        }
    }
}
