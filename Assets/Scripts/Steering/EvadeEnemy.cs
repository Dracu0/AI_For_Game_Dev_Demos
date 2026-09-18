using UnityEngine;

/// <summary>Flee the player's predicted position. Active inside evade range.</summary>
public class EvadeEnemy : SteeringEnemyBase
{
    [Header("Evade Range")]
    [SerializeField] Transform evadeRangeCircle;
    [SerializeField] float evadeRange = 5f;

    [Header("Prediction")]
    [SerializeField] float targetMaxSpeed = 5f;

    protected override void RefreshRangeVisuals() =>
        SteeringMath.ResizeCircle(evadeRangeCircle, evadeRange);

    protected override bool TryGetDesiredVelocity(out Vector2 desired)
    {
        if (IsPlayerBeyondRange(evadeRange))
        {
            desired = default;
            return false;
        }

        desired = SteeringMath.FleeVelocity(Body.position, PredictPlayerPosition(targetMaxSpeed), maxSpeed);
        return true;
    }
}
