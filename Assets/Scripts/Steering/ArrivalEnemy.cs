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
            SteeringMath.Stop(_rb);
            return;
        }

        Vector2 desiredVelocity = GetDesiredVelocity(distance);
        _rb.linearVelocity = SteeringMath.Steer(_rb, desiredVelocity, maxForce, maxSpeed);
    }

    Vector2 GetDesiredVelocity(float distance)
    {
        if (distance < stoppingDistance)
            return Vector2.zero;

        Vector2 direction = SteeringMath.Direction(_rb.position, player.position);
        if (distance < slowRadius)
            return direction * (maxSpeed * (distance / slowRadius));

        return direction * maxSpeed;
    }

    void UpdateRangeCircles()
    {
        SteeringMath.ResizeCircle(detectionRangeCircle, detectionRange);
        SteeringMath.ResizeCircle(slowRadiusCircle, slowRadius);
    }
}
