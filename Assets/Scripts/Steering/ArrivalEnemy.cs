using UnityEngine;

/// <summary>Slow down as the player is reached. Active inside detection range.</summary>
public class ArrivalEnemy : SteeringEnemyBase
{
    [Header("Detection")]
    [SerializeField] Transform detectionRangeCircle;
    [SerializeField] float detectionRange = 5f;

    [Header("Arrival")]
    [SerializeField] Transform slowRadiusCircle;
    [SerializeField] float slowRadius = 3f;

    protected override void RefreshRangeVisuals()
    {
        SteeringMath.ResizeCircle(detectionRangeCircle, detectionRange);
        SteeringMath.ResizeCircle(slowRadiusCircle, slowRadius);
    }

    protected override bool TryGetDesiredVelocity(out Vector2 desired)
    {
        float distance = Vector2.Distance(Body.position, player.position);
        if (distance > detectionRange)
        {
            desired = default;
            return false;
        }

        desired = SteeringMath.ArrivalVelocity(
            Body.position,
            player.position,
            distance,
            maxSpeed,
            slowRadius);
        return true;
    }
}
