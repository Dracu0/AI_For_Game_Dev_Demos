using UnityEngine;

/// <summary>
/// Flee — desiredVelocity = -seek desiredVelocity (away from the player).
/// Only active inside flee range.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FleeEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Flee Range")]
    [SerializeField] Transform fleeRangeCircle;
    [SerializeField] float fleeRange = 5f;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;
    SteeringCollisionAvoidance _avoidance;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _avoidance = GetComponent<SteeringCollisionAvoidance>();
        SteeringMath.SetupEnemy(_rb);
        SteeringMath.ResizeCircle(fleeRangeCircle, fleeRange);
    }

    void OnValidate()
    {
        SteeringMath.ResizeCircle(fleeRangeCircle, fleeRange);
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        if (SteeringMath.IsOutOfRange(_rb.position, player.position, fleeRange))
        {
            SteeringMath.Stop(_rb);
            return;
        }

        _rb.linearVelocity = SteeringMath.Steer(
            _rb,
            SteeringMath.FleeVelocity(_rb.position, player.position, maxSpeed),
            maxForce,
            maxSpeed,
            _avoidance);
    }
}
