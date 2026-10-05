using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the Player/Move and Player/Jump actions and exposes them as static values.
/// Movement scripts apply them; this class never touches a Rigidbody.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static Vector2 Movement { get; private set; }

    /// <summary>True from the frame Jump is pressed until a movement script calls ConsumeJump().</summary>
    public static bool JumpRequested { get; private set; }

    [SerializeField] InputActionAsset inputActions;

    InputActionAsset _runtimeActions;
    InputAction _moveAction;
    InputAction _jumpAction;

    void Awake() => TryBind();

    public void Bind(InputActionAsset asset)
    {
        inputActions = asset;
        TryBind();
    }

    /// <summary>Returns true once per jump press, so FixedUpdate scripts do not miss or repeat it.</summary>
    public static bool ConsumeJump()
    {
        bool requested = JumpRequested;
        JumpRequested = false;
        return requested;
    }

    void TryBind()
    {
        if (_runtimeActions != null || inputActions == null)
            return;

        // Clone so this demo does not share enabled state with the UI module.
        _runtimeActions = Instantiate(inputActions);
        _moveAction = _runtimeActions.FindAction("Player/Move", true);
        _jumpAction = _runtimeActions.FindAction("Player/Jump", true);
        _moveAction.Enable();
        _jumpAction.Enable();
    }

    void Update()
    {
        if (_moveAction == null)
            return;

        Movement = _moveAction.ReadValue<Vector2>();

        if (_jumpAction.WasPressedThisFrame())
            JumpRequested = true;
    }

    void OnDisable() => ResetState();

    void OnDestroy()
    {
        ResetState();

        if (_runtimeActions != null)
            Destroy(_runtimeActions);
    }

    void ResetState()
    {
        Movement = Vector2.zero;
        JumpRequested = false;
    }
}
