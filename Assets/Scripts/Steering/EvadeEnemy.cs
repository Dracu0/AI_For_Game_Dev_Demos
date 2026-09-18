using UnityEngine;

/// <summary>
/// Evade — flee the player's predicted position (same lookahead as pursue).
/// Only active inside evade range.
/// </summary>
public class EvadeEnemy : SteeringEnemyBase
{
    [Header("Evade Range")]
    [SerializeField] Transform evadeRangeCircle;
    [SerializeField] float evadeRange = 5f;

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
        SteeringMath.ResizeCircle(evadeRangeCircle, evadeRange);

    protected override bool TryGetDesiredVelocity(out Vector2 desired)
    {
        if (SteeringMath.IsOutOfRange(Body.position, player.position, evadeRange))
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

        desired = SteeringMath.FleeVelocity(Body.position, predicted, maxSpeed);
        return true;
    }
}
