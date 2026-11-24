using UnityEngine;

/// <summary>
/// Side-view movement: horizontal input sets velocity.x, Jump sets velocity.y when grounded.
/// Gravity comes from the Rigidbody2D's Gravity Scale.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovementPlatformer : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpSpeed = 10f;

    [Header("Ground Check")]
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] float groundCheckDistance = 0.05f;

    Rigidbody2D _rb;
    Collider2D _collider;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        Vector2 velocity = _rb.linearVelocity;
        velocity.x = InputManager.Movement.x * moveSpeed;

        // Always consume so a press made in the air does not fire on landing.
        if (InputManager.ConsumeJump() && IsGrounded())
            velocity.y = jumpSpeed;

        _rb.linearVelocity = velocity;
    }

    bool IsGrounded()
    {
        Bounds bounds = _collider.bounds;
        Vector2 size = new(bounds.size.x * 0.9f, groundCheckDistance);
        Vector2 center = new(bounds.center.x, bounds.min.y - groundCheckDistance * 0.5f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, groundLayers);
        foreach (Collider2D hit in hits)
            if (hit != _collider && !hit.isTrigger)
                return true;

        return false;
    }
}
