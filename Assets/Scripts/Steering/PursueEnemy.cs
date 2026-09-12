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
        _rb.gravityScale = 0f;
        UpdateRangeCircle();
    }

    void Start()
    {
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
    }

    void OnValidate()
    {
        UpdateRangeCircle();
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        if (Vector2.Distance(_rb.position, player.position) > pursueRange)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition = GetPredictedPlayerPosition();
        Vector2 toTarget = targetPosition - _rb.position;
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        Vector2 desiredVelocity = toTarget.normalized * maxSpeed;
        Vector2 steering = Vector2.ClampMagnitude(
            desiredVelocity - _rb.linearVelocity,
            maxForce);

        Vector2 velocity = _rb.linearVelocity + steering * Time.fixedDeltaTime;
        _rb.linearVelocity = Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    Vector2 GetPredictedPlayerPosition()
    {
        Vector2 playerPosition = player.position;
        if (_playerRb == null)
            return playerPosition;

        Vector2 toPlayer = playerPosition - _rb.position;
        float distance = toPlayer.magnitude;
        float lookAheadTime = distance / maxSpeed;

        return playerPosition + _playerRb.linearVelocity * lookAheadTime;
    }

    void UpdateRangeCircle()
    {
        if (pursueRangeCircle == null)
            return;

        SpriteRenderer sprite = pursueRangeCircle.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return;

        float diameter = sprite.sprite.bounds.size.x;
        if (diameter <= 0f)
            return;

        pursueRangeCircle.localScale = Vector3.one * (pursueRange * 2f / diameter);
    }
}
