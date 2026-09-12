using UnityEngine;

/// <summary>
/// Arrival — reach the player and stop smoothly.
///
/// Goal: move toward the player without overshooting.
///
/// Outside detection range: stop (idle).
/// Far but inside detection: full speed toward the player.
/// Inside slow radius: speed scales down with distance.
/// Inside stopping distance: stop.
///
/// Use when: enemy or NPC should walk up to the player and halt
/// (guard, shopkeeper, ally).
///
/// vs Seek: arrival slows down near the target; seek does not.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArrivalEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Detection")]
    [SerializeField] Transform detectionRangeCircle;
    [SerializeField] float detectionRange = 5f;

    [Header("Arrival")]
    [SerializeField] Transform slowRadiusCircle;
    [SerializeField] float slowRadius = 3f;
    [SerializeField] float stoppingDistance = 0.25f;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        UpdateRangeCircles();
    }

    void OnValidate()
    {
        UpdateRangeCircles();
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(_rb.position, player.position);

        if (distance > detectionRange)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - _rb.position;

        Vector2 desiredVelocity;
        if (distance < stoppingDistance)
        {
            desiredVelocity = Vector2.zero;
        }
        else if (distance < slowRadius)
        {
            desiredVelocity = toPlayer / distance * (maxSpeed * (distance / slowRadius));
        }
        else
        {
            desiredVelocity = toPlayer / distance * maxSpeed;
        }

        Vector2 steering = Vector2.ClampMagnitude(
            desiredVelocity - _rb.linearVelocity,
            maxForce);

        Vector2 velocity = _rb.linearVelocity + steering * Time.fixedDeltaTime;
        _rb.linearVelocity = Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    void UpdateRangeCircles()
    {
        ResizeCircle(detectionRangeCircle, detectionRange);
        ResizeCircle(slowRadiusCircle, slowRadius);
    }

    void ResizeCircle(Transform circle, float radius)
    {
        if (circle == null)
            return;

        SpriteRenderer sprite = circle.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return;

        float diameter = sprite.sprite.bounds.size.x;
        if (diameter <= 0f)
            return;

        circle.localScale = Vector3.one * (radius * 2f / diameter);
    }
}
