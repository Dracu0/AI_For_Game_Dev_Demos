using UnityEngine;

/// <summary>
/// Flee — desiredVelocity = -seek (away from the player).
/// Only active inside flee range.
/// </summary>
public class FleeEnemy : SteeringEnemyBase
{
    [Header("Flee Range")]
    [SerializeField] Transform fleeRangeCircle;
    [SerializeField] float fleeRange = 5f;

    protected override void RefreshRangeVisuals() =>
        SteeringMath.ResizeCircle(fleeRangeCircle, fleeRange);

    protected override bool TryGetDesiredVelocity(out Vector2 desired)
    {
        if (SteeringMath.IsOutOfRange(Body.position, player.position, fleeRange))
        {
            desired = default;
            return false;
        }

        desired = SteeringMath.FleeVelocity(Body.position, player.position, maxSpeed);
        return true;
    }
}
