using UnityEngine;

/// <summary>
/// Pursue — seek the player's predicted position (lookahead T = distance / targetMaxSpeed).
/// Only active inside pursue range.
/// </summary>
public class PursueEnemy : SteeringEnemyBase
{
    [Header("Pursue Range")]
    [SerializeField] Transform pursueRangeCircle;
    [SerializeField] float pursueRange = 8f;

    [Header("Prediction")]
    [SerializeField] float targetMaxSpeed = 5f;

    Rigidbody2D _playerRb;

    protected override void Awake()
    {
        base.Awake();
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
    }

    protected override void RefreshRangeVisuals() =>
        SteeringMath.ResizeCircle(pursueRangeCircle, pursueRange);

    protected override bool TryGetDesiredVelocity(out Vector2 desired)
    {
        if (SteeringMath.IsOutOfRange(Body.position, player.position, pursueRange))
        {
            desired = default;
            return false;
        }

        Vector2 targetVelocity = _playerRb != null ? _playerRb.linearVelocity : Vector2.zero;
        Vector2 predicted = SteeringMath.PredictPosition(
            Body.position,
            player.position,
            targetVelocity,
            targetMaxSpeed);

        desired = SteeringMath.SeekVelocity(Body.position, predicted, maxSpeed);
        return true;
    }
}
