using UnityEngine;

/// <summary>
/// Evade — flee the player's predicted position (same lookahead as pursue).
/// Only active inside evade range.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EvadeEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Evade Range")]
    [SerializeField] Transform evadeRangeCircle;
    [SerializeField] float evadeRange = 5f;

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
        SteeringMath.ResizeCircle(evadeRangeCircle, evadeRange);
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
    }

    void OnValidate()
    {
        SteeringMath.ResizeCircle(evadeRangeCircle, evadeRange);
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        if (SteeringMath.IsOutOfRange(_rb.position, player.position, evadeRange))
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
            SteeringMath.FleeVelocity(_rb.position, predictedPosition, maxSpeed),
            maxForce,
            maxSpeed,
            _avoidance);
    }
}
