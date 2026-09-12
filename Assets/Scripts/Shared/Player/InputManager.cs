using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static Vector2 Movement { get; private set; }

    [SerializeField] InputActionAsset inputActions;

    InputActionMap _playerMap;
    InputAction _moveAction;
    bool _usesPlayerInput;

    void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            _usesPlayerInput = true;
            _moveAction = playerInput.actions.FindAction("Move", true);
            return;
        }

        if (inputActions == null)
        {
            Debug.LogError(
                "InputManager: assign Input Actions on this component, or add a PlayerInput component.",
                this);
            return;
        }

        _playerMap = inputActions.FindActionMap("Player", true);
        _moveAction = _playerMap.FindAction("Move", true);
    }

    void OnEnable()
    {
        if (_usesPlayerInput)
            return;

        _playerMap?.Enable();
    }

    void OnDisable()
    {
        if (_usesPlayerInput)
            return;

        _playerMap?.Disable();
    }

    void Update()
    {
        Movement = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
    }
}
