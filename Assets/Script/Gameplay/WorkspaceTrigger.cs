using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Detects when the player enters a 2D trigger near a workspace.
/// While inside, the interact action opens the assigned UI panel.
/// Leaving the trigger hides the panel (and the prompt).
/// Requires a Collider2D on this object with "Is Trigger" enabled.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WorkspaceTrigger : MonoBehaviour
{
    private enum OpenMode
    {
        OnInteract,
        OnEnter
    }

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";

    [Header("UI")]
    [Tooltip("The UI panel this workspace opens.")]
    [SerializeField] private GameObject workspacePanel;
    [Tooltip("Optional: 'Press E to use' style prompt shown while in range.")]
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private OpenMode openMode = OpenMode.OnInteract;

    [Header("Input")]
    [Tooltip("Optional: assign an Interact action. If empty, defaults to E / Gamepad South.")]
    [SerializeField] private InputActionReference interactActionReference;

    [Header("Events")]
    [SerializeField] private UnityEvent onPanelOpened;
    [SerializeField] private UnityEvent onPanelClosed;

    private InputAction interactAction;
    private bool ownsAction;
    private bool playerInRange;
    private bool panelOpen;

    private void Awake()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        if (interactActionReference != null)
        {
            interactAction = interactActionReference.action;
            ownsAction = false;
        }
        else
        {
            interactAction = new InputAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonSouth");
            ownsAction = true;
        }

        SetActiveSafe(workspacePanel, false);
        SetActiveSafe(interactPrompt, false);
    }

    private void OnEnable()
    {
        interactAction.performed += OnInteract;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.performed -= OnInteract;

        if (ownsAction)
        {
            interactAction.Disable();
        }
    }

    private void OnDestroy()
    {
        if (ownsAction)
        {
            interactAction.Dispose();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = true;

        switch (openMode)
        {
            case OpenMode.OnEnter:
                OpenPanel();
                break;
            case OpenMode.OnInteract:
                SetActiveSafe(interactPrompt, true);
                break;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = false;
        SetActiveSafe(interactPrompt, false);
        ClosePanel();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (!playerInRange)
        {
            return;
        }

        // Interact toggles the panel while in range.
        if (panelOpen)
        {
            ClosePanel();
            SetActiveSafe(interactPrompt, true);
        }
        else
        {
            OpenPanel();
        }
    }

    public void OpenPanel()
    {
        if (panelOpen)
        {
            return;
        }

        panelOpen = true;
        SetActiveSafe(interactPrompt, false);
        SetActiveSafe(workspacePanel, true);
        onPanelOpened.Invoke();
    }

    public void ClosePanel()
    {
        if (!panelOpen)
        {
            return;
        }

        panelOpen = false;
        SetActiveSafe(workspacePanel, false);
        onPanelClosed.Invoke();
    }

    private static void SetActiveSafe(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
