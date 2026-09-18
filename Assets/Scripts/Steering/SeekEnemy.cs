using UnityEngine;

/// <summary>
/// Seek — desiredVelocity = normalize(target - position) * maxSpeed.
/// Only active inside seek range.
/// </summary>
public class SeekEnemy : SteeringEnemyBase
{
    [Header("Seek Range")]
    [SerializeField] Transform seekRangeCircle;
    [SerializeField] float seekRange = 5f;

    protected override void RefreshRangeVisuals() =>
        SteeringMath.ResizeCircle(seekRangeCircle, seekRange);

    protected override bool TryGetDesiredVelocity(out Vector2 desired)
    {
        if (SteeringMath.IsOutOfRange(Body.position, player.position, seekRange))
        {
            desired = default;
            return false;
        }

        desired = SteeringMath.SeekVelocity(Body.position, player.position, maxSpeed);
        return true;
    }
}
