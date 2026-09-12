using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static Vector2 Movement { get; private set; }

    [SerializeField] private InputActionAsset inputActions;

    private InputAction _moveAction;

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

        _moveAction = actions.FindAction("Player/Move", true);
        actions.FindActionMap("Player", true).Enable();
    }

    void Update()
    {
        Movement = _moveAction != null
            ? _moveAction.ReadValue<Vector2>()
            : Vector2.zero;
    }
}