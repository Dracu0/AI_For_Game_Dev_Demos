using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the Player/Move action and stores it for other scripts.
/// PlayerMovement should be the only script that applies this to a Rigidbody.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static Vector2 Movement { get; private set; }

    [SerializeField] InputActionAsset inputActions;

    InputActionAsset _runtimeActions;
    InputAction _moveAction;
    bool _ownsActionLifecycle;

    void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            _moveAction = playerInput.actions.FindAction("Player/Move", true);
            _ownsActionLifecycle = false;
            return;
        }

        if (inputActions == null)
        {
            Debug.LogError(
                "InputManager requires a PlayerInput component or an assigned Input Actions asset.",
                this);
            return;
        }

        _runtimeActions = Instantiate(inputActions);
        _moveAction = _runtimeActions.FindAction("Player/Move", true);
        _moveAction.Enable();
        _ownsActionLifecycle = true;
    }

    void OnDisable()
    {
        Movement = Vector2.zero;
    }

    void OnDestroy()
    {
        Movement = Vector2.zero;

        if (!_ownsActionLifecycle || _moveAction == null)
            return;

        _moveAction.Disable();

        if (_runtimeActions != null)
            Destroy(_runtimeActions);
    }

    void Update()
    {
        Movement = _moveAction != null
            ? _moveAction.ReadValue<Vector2>()
            : Vector2.zero;
    }
}
