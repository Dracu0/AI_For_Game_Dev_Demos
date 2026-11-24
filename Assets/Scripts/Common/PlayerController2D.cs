using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [SerializeField] private InputActionAsset controls;

    private InputActionMap playerMap;
    private InputAction moveAction;
    private Vector2 moveInput;
    private Rigidbody2D _rb;

    [SerializeField] private float moveSpeed = 5f;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        playerMap = controls.FindActionMap("Player2D", true);
        moveAction = playerMap.FindAction("Move", true);
    }

    private void OnEnable()
    {
        if (playerMap != null)
            playerMap.Enable();
    }

    private void OnDisable()
    {
        if (playerMap != null)
            playerMap.Disable();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = moveInput.normalized * moveSpeed;
    }
}