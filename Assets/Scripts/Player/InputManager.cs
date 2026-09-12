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

    InputActionMap _playerMap;
    InputAction _moveAction;

    void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        InputActionAsset actions = playerInput != null
            ? playerInput.actions
            : inputActions;

        if (actions == null)
        {
            Debug.LogError(
                "InputManager requires a PlayerInput component or an assigned Input Actions asset.",
                this);
            return;
        }

        _playerMap = actions.FindActionMap("Player", true);
        _moveAction = _playerMap.FindAction("Move", true);
    }

    void OnEnable()
    {
        _playerMap?.Enable();
    }

    void OnDisable()
    {
        _playerMap?.Disable();
        Movement = Vector2.zero;
    }

    void Update()
    {
        Movement = _moveAction != null
            ? _moveAction.ReadValue<Vector2>()
            : Vector2.zero;
    }
}
