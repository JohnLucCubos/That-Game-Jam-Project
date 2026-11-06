using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Optional: implement on any component that should react to being selected.
/// </summary>
public interface ISelectable
{
    void OnSelected();
    void OnDeselected();
}

/// <summary>
/// Click-to-select, click-and-drag, and drop-into-slot via camera raycast.
/// Put this on one GameObject in the scene.
/// Checks 2D colliders first, then 3D colliders.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private float maxDistance = 1000f;
    [SerializeField] private bool deselectOnEmptyClick = true;
    [SerializeField] private bool enableDragging = true;
    [SerializeField] private bool debugLogging = false;

    public GameObject Current { get; private set; }
    public bool IsDragging { get; private set; }

    // Fired with (previous, current). Either can be null.
    public event Action<GameObject, GameObject> SelectionChanged;

    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private Transform dragTarget;
    private Rigidbody2D dragBody2D;
    private SlottableItem dragItem;
    private Plane dragPlane;
    private Vector3 dragOffset;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogError("SelectionManager: no camera assigned and none tagged MainCamera.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (IsDragging)
        {
            UpdateDrag();
            return;
        }

        if (WasPointerPressed())
        {
            HandlePress();
        }
    }

    private void HandlePress()
    {
        Vector2 pointer = GetPointerPosition();

        if (IsPointerOverUI(pointer))
        {
            Log("Click blocked by UI.");
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(pointer);
        // Slots are never selected, so clicks pass through them to items.
        GameObject hitObject = FirstHit(ray, IsNotSlot);

        if (hitObject != null)
        {
            Log("Hit: " + hitObject.name);
            Select(hitObject);

            if (enableDragging)
            {
                BeginDrag(hitObject, ray);
            }
        }
        else
        {
            Log("Hit nothing.");
            if (deselectOnEmptyClick)
            {
                Select(null);
            }
        }
    }

    // ---------- Dragging ----------

    private void BeginDrag(GameObject hitObject, Ray ray)
    {
        dragTarget = ResolveDragRoot(hitObject);
        dragBody2D = dragTarget.GetComponent<Rigidbody2D>();
        dragItem = dragTarget.GetComponent<SlottableItem>();

        if (dragItem != null)
        {
            dragItem.BeginDrag();
        }

        // Plane facing the camera, through the object. Keeps the object's
        // depth (z in 2D) fixed. Works for orthographic and perspective cameras.
        dragPlane = new Plane(-targetCamera.transform.forward, dragTarget.position);

        if (dragPlane.Raycast(ray, out float enter))
        {
            dragOffset = dragTarget.position - ray.GetPoint(enter);
        }
        else
        {
            dragOffset = Vector3.zero;
        }

        IsDragging = true;
    }

    private void UpdateDrag()
    {
        if (dragTarget == null)
        {
            EndDrag();
            return;
        }

        if (!IsPointerHeld())
        {
            DropItem();
            EndDrag();
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(GetPointerPosition());
        if (!dragPlane.Raycast(ray, out float enter))
        {
            return;
        }

        Vector3 targetPosition = ray.GetPoint(enter) + dragOffset;

        if (dragBody2D != null)
        {
            dragBody2D.position = targetPosition;
            dragBody2D.linearVelocity = Vector2.zero;
        }
        else
        {
            dragTarget.position = targetPosition;
        }
    }

    private void DropItem()
    {
        if (dragItem == null)
        {
            // Not slot-aware: stays where it was dropped.
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(GetPointerPosition());
        GameObject slotObject = FirstHit(ray, IsSlot);
        ItemSlot slot = null;

        if (slotObject != null)
        {
            slot = slotObject.GetComponent<ItemSlot>();
        }

        bool placed = dragItem.TryPlace(slot);
        if (placed)
        {
            Log("Placed " + dragItem.name + " in " + slot.name);
        }
        else
        {
            Log("No valid slot. Returning " + dragItem.name);
        }
    }

    private void EndDrag()
    {
        IsDragging = false;
        dragTarget = null;
        dragBody2D = null;
        dragItem = null;
    }

    // Drag the object that owns ISelectable, not a child collider.
    private static Transform ResolveDragRoot(GameObject hitObject)
    {
        ISelectable selectable = hitObject.GetComponentInParent<ISelectable>();
        if (selectable is Component component)
        {
            return component.transform;
        }
        return hitObject.transform;
    }

    // ---------- Selection ----------

    public void Select(GameObject target)
    {
        if (target == Current)
        {
            return;
        }

        GameObject previous = Current;

        if (previous != null)
        {
            foreach (ISelectable s in previous.GetComponentsInParent<ISelectable>())
            {
                s.OnDeselected();
            }
        }

        Current = target;

        if (Current != null)
        {
            foreach (ISelectable s in Current.GetComponentsInParent<ISelectable>())
            {
                s.OnSelected();
            }
        }

        SelectionChanged?.Invoke(previous, Current);
    }

    // ---------- Raycasts ----------

    // Returns the nearest hit object that passes the filter. 2D first, then 3D.
    private GameObject FirstHit(Ray ray, Predicate<GameObject> accept)
    {
        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray, maxDistance, raycastLayers);
        foreach (RaycastHit2D hit in hits2D)
        {
            if (accept(hit.collider.gameObject))
            {
                return hit.collider.gameObject;
            }
        }

        RaycastHit[] hits3D = Physics.RaycastAll(ray, maxDistance, raycastLayers);
        Array.Sort(hits3D, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits3D)
        {
            if (accept(hit.collider.gameObject))
            {
                return hit.collider.gameObject;
            }
        }

        return null;
    }

    private static bool IsSlot(GameObject go)
    {
        return go.GetComponent<ItemSlot>() != null;
    }

    private static bool IsNotSlot(GameObject go)
    {
        return go.GetComponent<ItemSlot>() == null;
    }

    // Only counts actual UI (Canvas graphics). EventSystem.IsPointerOverGameObject()
    // also returns true for sprites when the camera has a Physics2DRaycaster.
    private bool IsPointerOverUI(Vector2 pointer)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData data = new PointerEventData(EventSystem.current);
        data.position = pointer;
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);

        foreach (RaycastResult result in uiHits)
        {
            if (result.module is GraphicRaycaster)
            {
                return true;
            }
        }
        return false;
    }

    // ---------- Input ----------

    private void Log(string message)
    {
        if (debugLogging)
        {
            Debug.Log("SelectionManager: " + message, this);
        }
    }

    private static bool WasPointerPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static bool IsPointerHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    private static Vector2 GetPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current.position.ReadValue();
#else
        return Input.mousePosition;
#endif
    }
}
