using UnityEngine;

/// <summary>
/// Flee — run away from the player at full speed.
///
/// Goal: escape the player when they get too close.
/// Formula: desiredVelocity = directionAwayFromPlayer * maxSpeed
///
/// Only active inside flee range. Same steering math as seek, but the
/// direction is flipped.
///
/// Use when: cowardly NPC, scared animal, enemy retreating.
///
/// vs Seek: flee moves away from the player, not toward them.
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

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
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

        SteeringMath.SteerIfMoving(
            _rb,
            SteeringMath.FleeVelocity(_rb.position, player.position, maxSpeed),
            maxForce,
            maxSpeed);
    }
}
