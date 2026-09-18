using UnityEngine;

/// <summary>Run from the player. Active inside flee range.</summary>
public class FleeEnemy : SteeringEnemyBase
{
    [Header("Flee Range")]
    [SerializeField] Transform fleeRangeCircle;
    [SerializeField] float fleeRange = 5f;

    protected override void RefreshRangeVisuals() =>
        SteeringMath.ResizeCircle(fleeRangeCircle, fleeRange);

    protected override bool TryGetDesiredVelocity(out Vector2 desired)
    {
        if (IsPlayerBeyondRange(fleeRange))
        {
            desired = default;
            return false;
        }

        desired = SteeringMath.FleeVelocity(Body.position, player.position, maxSpeed);
        return true;
    }
}
