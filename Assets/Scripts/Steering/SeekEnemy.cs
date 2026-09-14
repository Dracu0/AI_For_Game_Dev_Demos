using UnityEngine;

/// <summary>
/// Seek — chase the player's current position at full speed.
///
/// Goal: reach the player as fast as possible.
/// Formula: desiredVelocity = directionToPlayer * maxSpeed
///
/// Only active inside seek range. Never slows down, so it may overshoot
/// or orbit if it gets close.
///
/// Use when: simple chaser that does not need to stop neatly (zombie, basic enemy).
///
/// vs Pursue: seek aims at where the player is now.
/// vs Arrival: seek never slows down near the target.
/// vs Flee: seek moves toward the player, not away.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SeekEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Seek Range")]
    [SerializeField] Transform seekRangeCircle;
    [SerializeField] float seekRange = 5f;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        SteeringMath.SetupEnemy(_rb);
        SteeringMath.ResizeCircle(seekRangeCircle, seekRange);
    }

    void OnValidate()
    {
        SteeringMath.ResizeCircle(seekRangeCircle, seekRange);
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        if (SteeringMath.IsOutOfRange(_rb.position, player.position, seekRange))
        {
            SteeringMath.Stop(_rb);
            return;
        }

        SteeringMath.SteerIfMoving(
            _rb,
            SteeringMath.SeekVelocity(_rb.position, player.position, maxSpeed),
            maxForce,
            maxSpeed);
    }
}
