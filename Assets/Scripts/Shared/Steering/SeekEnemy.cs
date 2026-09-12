using UnityEngine;

/// <summary>
/// Basic seek behaviour for a 2D enemy.
/// The enemy follows the assigned player target.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SeekEnemy : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        Vector2 toPlayer = (Vector2)player.position - _rb.position;
        Vector2 desiredVelocity = toPlayer.normalized * maxSpeed;
        Vector2 steering = Vector2.ClampMagnitude(
            desiredVelocity - _rb.linearVelocity,
            maxForce);


        Vector2 velocity = _rb.linearVelocity + steering * Time.fixedDeltaTime;
        _rb.linearVelocity = Vector2.ClampMagnitude(velocity, maxSpeed);
    }
}


