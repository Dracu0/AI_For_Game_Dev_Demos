using UnityEngine;

/// <summary>
/// Basic seek behaviour for a 2D enemy.
/// The enemy follows the assigned player target.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SeekEnemy : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform seekRangeCircle;
    [SerializeField] float seekRange = 5f;
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        UpdateSeekRangeCircle();
    }

    void OnValidate()
    {
        UpdateSeekRangeCircle();
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        Vector2 toPlayer = (Vector2)player.position - _rb.position;

        if (toPlayer.sqrMagnitude > seekRange * seekRange)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 desiredVelocity = toPlayer.normalized * maxSpeed;
        Vector2 steering = Vector2.ClampMagnitude(
            desiredVelocity - _rb.linearVelocity,
            maxForce);


        Vector2 velocity = _rb.linearVelocity + steering * Time.fixedDeltaTime;
        _rb.linearVelocity = Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    void UpdateSeekRangeCircle()
    {
        if (seekRangeCircle == null)
            return;

        SpriteRenderer circleRenderer = seekRangeCircle.GetComponent<SpriteRenderer>();
        if (circleRenderer == null || circleRenderer.sprite == null)
            return;

        float diameter = circleRenderer.sprite.bounds.size.x;
        if (diameter <= 0f)
            return;

        float scale = (seekRange * 2f) / diameter;
        seekRangeCircle.localScale = Vector3.one * scale;
    }
}


