using UnityEngine;

/// <summary>
/// Pursue — chase a moving player by aiming ahead of them.
///
/// Goal: intercept the player, not just follow their current position.
/// Formula:
///   lookAheadTime = distanceToPlayer / maxSpeed
///   predictedPosition = playerPosition + playerVelocity * lookAheadTime
///   desiredVelocity = directionToPredictedPosition * maxSpeed
///
/// Only active inside pursue range. Works best when the player has a Rigidbody2D.
///
/// Use when: intercepting a moving target (predator, missile-like enemy).
///
/// vs Seek: pursue leads the target; seek chases where they are now.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PursueEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Pursue Range")]
    [SerializeField] Transform pursueRangeCircle;
    [SerializeField] float pursueRange = 8f;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;
    Rigidbody2D _playerRb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        SteeringMath.SetupEnemy(_rb);
        SteeringMath.ResizeCircle(pursueRangeCircle, pursueRange);
    }

    void Start()
    {
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
    }

    void OnValidate()
    {
        SteeringMath.ResizeCircle(pursueRangeCircle, pursueRange);
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        if (SteeringMath.IsOutOfRange(_rb.position, player.position, pursueRange))
        {
            SteeringMath.Stop(_rb);
            return;
        }

        Vector2 targetVelocity = _playerRb != null ? _playerRb.linearVelocity : Vector2.zero;
        Vector2 predictedPosition = SteeringMath.PredictPosition(
            _rb.position,
            player.position,
            targetVelocity,
            maxSpeed);

        SteeringMath.SteerIfMoving(
            _rb,
            SteeringMath.SeekVelocity(_rb.position, predictedPosition, maxSpeed),
            maxForce,
            maxSpeed);
    }
}
