using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the Player/Move action. PlayerMovement is the only script that
/// should apply this value to a Rigidbody.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static Vector2 Movement { get; private set; }

    [SerializeField] InputActionAsset inputActions;

    InputActionAsset _runtimeActions;
    InputAction _moveAction;

    void Awake() => TryBind();

    public void Bind(InputActionAsset asset)
    {
        inputActions = asset;
        TryBind();
    }

    void TryBind()
    {
        if (_moveAction != null || inputActions == null)
            return;

        // Clone so this demo does not share enabled state with the UI module.
        _runtimeActions = Instantiate(inputActions);
        _moveAction = _runtimeActions.FindAction("Player/Move", true);
        _moveAction.Enable();
    }

    void OnDisable() => Movement = Vector2.zero;

    void OnDestroy()
    {
        Movement = Vector2.zero;

        if (_moveAction != null)
            _moveAction.Disable();

        if (_runtimeActions != null)
            Destroy(_runtimeActions);
    }

    void Update() =>
        Movement = _moveAction != null
            ? _moveAction.ReadValue<Vector2>()
            : Vector2.zero;
}
