using UnityEngine;

/// <summary>Seek the player's predicted position. Active inside pursue range.</summary>
public class PursueEnemy : SteeringEnemyBase
{
    [Header("Pursue Range")]
    [SerializeField] Transform pursueRangeCircle;
    [SerializeField] float pursueRange = 8f;

    [Header("Prediction")]
    [SerializeField] float targetMaxSpeed = 5f;

    protected override void RefreshRangeVisuals() =>
        SteeringMath.ResizeCircle(pursueRangeCircle, pursueRange);

    protected override bool TryGetDesiredVelocity(out Vector2 desired)
    {
        if (IsPlayerBeyondRange(pursueRange))
        {
            desired = default;
            return false;
        }

        desired = SteeringMath.SeekVelocity(Body.position, PredictPlayerPosition(targetMaxSpeed), maxSpeed);
        return true;
    }
}
