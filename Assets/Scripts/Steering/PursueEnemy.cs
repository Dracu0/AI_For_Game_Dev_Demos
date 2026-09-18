using UnityEngine;

/// <summary>
/// Pursue — seek the player's predicted position (lookahead T = distance / targetMaxSpeed).
/// Only active inside pursue range.
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
    [SerializeField] float targetMaxSpeed = 5f;

    Rigidbody2D _rb;
    Rigidbody2D _playerRb;
    SteeringCollisionAvoidance _avoidance;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _avoidance = GetComponent<SteeringCollisionAvoidance>();
        SteeringMath.SetupEnemy(_rb);
        SteeringMath.ResizeCircle(pursueRangeCircle, pursueRange);
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
            targetMaxSpeed);

        _rb.linearVelocity = SteeringMath.Steer(
            _rb,
            SteeringMath.SeekVelocity(_rb.position, predictedPosition, maxSpeed),
            maxForce,
            maxSpeed,
            _avoidance);
    }
}
