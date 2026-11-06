using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// X-axis-only side-scroll movement using the new Input System (Unity 6).
/// Requires a Rigidbody2D. Y velocity is left to physics (gravity, jumps, etc.).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SideScrollMovement : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Optional: assign a 1D Axis or Vector2 action from an Input Actions asset. " +
             "If left empty, a default A/D, Arrow keys, and Gamepad stick binding is used.")]
    [SerializeField] private InputActionReference moveActionReference;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 80f;
    [SerializeField] private bool flipSpriteWithDirection = true;

    [Header("Animation")]
    [Tooltip("Optional: auto-found on this object or its children if left empty.")]
    [SerializeField] private Animator animator;
    [Tooltip("Smooths the Speed parameter so blend trees don't jitter. 0 = no smoothing.")]
    [SerializeField] private float speedDampTime = 0.05f;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    private Rigidbody2D body;
    private InputAction moveAction;
    private bool ownsAction;
    private float moveInput;
    private bool facingRight = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.freezeRotation = true;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (moveActionReference != null)
        {
            moveAction = moveActionReference.action;
            ownsAction = false;
        }
        else
        {
            moveAction = CreateDefaultMoveAction();
            ownsAction = true;
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        // Only disable actions we created; shared asset actions may be used elsewhere.
        if (ownsAction)
        {
            moveAction.Disable();
        }
    }

    private void OnDestroy()
    {
        if (ownsAction)
        {
            moveAction.Dispose();
        }
    }

    private void Update()
    {
        moveInput = ReadHorizontalInput();

        if (flipSpriteWithDirection)
        {
            UpdateFacing();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Vector2 velocity = body.linearVelocity;
        float targetSpeed = moveInput * maxSpeed;

        float rate;
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            rate = acceleration;
        }
        else
        {
            rate = deceleration;
        }

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        body.linearVelocity = velocity;
    }

    private float ReadHorizontalInput()
    {
        // Supports both 1D Axis (float) and Vector2 actions; only X is used.
        if (moveAction.expectedControlType == "Vector2")
        {
            return moveAction.ReadValue<Vector2>().x;
        }

        return moveAction.ReadValue<float>();
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        // Use actual velocity (not input) so walls/knockback read correctly.
        float speed = Mathf.Abs(body.linearVelocity.x);
        animator.SetFloat(SpeedParam, speed, speedDampTime, Time.deltaTime);
    }

    private void UpdateFacing()
    {
        if (moveInput > 0.01f && !facingRight)
        {
            Flip();
        }
        else if (moveInput < -0.01f && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private static InputAction CreateDefaultMoveAction()
    {
        InputAction action = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");

        action.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");

        action.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");

        action.AddBinding("<Gamepad>/leftStick/x");

        return action;
    }
}