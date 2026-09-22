using UnityEngine;

/// <summary>
/// Look-ahead obstacle avoidance.
///
/// Cast a circle along the movement line. If a wall is in the way, steer away
/// from it — or slide along it when heading into it. If the agent is already
/// overlapping a wall, cancel the velocity that would push deeper.
///
/// https://code.tutsplus.com/understanding-steering-behaviors-collision-avoidance--gamedev-7777t
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    // Cosine of 45°. More head-on than this, and the agent slides along the wall.
    const float SlideWhenFacingWall = 0.7f;

    [SerializeField] LayerMask obstacleLayers = 1 << 3;
    [SerializeField] float maxSeeAhead = 3f;
    [SerializeField] float maxAvoidForce = 12f;
    [Tooltip("Extra radius so agents keep a small gap from walls.")]
    [SerializeField] float obstaclePadding = 0.4f;

    CircleCollider2D body;

    void Awake() => body = GetComponent<CircleCollider2D>();

    public Vector2 GetAvoidanceForce(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed)
    {
        Vector2 direction = velocity.sqrMagnitude > SteeringMath.Epsilon ? velocity : desiredVelocity;
        if (direction.sqrMagnitude < SteeringMath.Epsilon)
            return Vector2.zero;

        direction.Normalize();
        float lookDistance = LookAheadDistance(velocity, desiredVelocity, maxSpeed);

        if (!TryFindWall(position, direction, lookDistance, out Vector2 awayFromWall))
            return Vector2.zero;

        return AvoidForce(awayFromWall, desiredVelocity);
    }

    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < SteeringMath.Epsilon)
            return velocity;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, BodyRadius(), obstacleLayers);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == body)
                continue;

            Vector2 outOfWall = position - wall.ClosestPoint(position);
            if (outOfWall.sqrMagnitude < SteeringMath.Epsilon)
                continue;

            outOfWall.Normalize();

            // Remove the part of velocity that points into the wall.
            float pushingIn = Vector2.Dot(velocity, -outOfWall);
            if (pushingIn > 0f)
                velocity += outOfWall * pushingIn;
        }

        return velocity;
    }

    float LookAheadDistance(Vector2 velocity, Vector2 desiredVelocity, float maxSpeed)
    {
        float distance = maxSpeed > SteeringMath.Epsilon
            ? maxSeeAhead * Mathf.Clamp01(velocity.magnitude / maxSpeed)
            : 0f;

        // Still look ahead while speeding up, or walls are only noticed once the agent is fast.
        if (desiredVelocity.sqrMagnitude > SteeringMath.Epsilon)
            distance = Mathf.Max(distance, maxSeeAhead * 0.5f);

        return distance;
    }

    bool TryFindWall(Vector2 position, Vector2 direction, float lookDistance, out Vector2 awayFromWall)
    {
        float radius = BodyRadius() + obstaclePadding;

        // CircleCast starts outside the body, so it misses a wall we are already touching.
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, radius, obstacleLayers);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == body)
                continue;

            Vector2 away = position - wall.ClosestPoint(position);
            awayFromWall = away.sqrMagnitude > SteeringMath.Epsilon ? away.normalized : Vector2.up;
            return true;
        }

        RaycastHit2D hit = Physics2D.CircleCast(position, radius, direction, lookDistance, obstacleLayers);
        if (hit.collider == null || hit.collider == body || hit.normal.sqrMagnitude < SteeringMath.Epsilon)
        {
            awayFromWall = default;
            return false;
        }

        awayFromWall = hit.normal.normalized;
        return true;
    }

    Vector2 AvoidForce(Vector2 awayFromWall, Vector2 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude < SteeringMath.Epsilon)
            return awayFromWall * maxAvoidForce;

        // Aimed into the wall: keep the part of the desired velocity that runs along it.
        Vector2 intoWall = -awayFromWall;
        if (Vector2.Dot(desiredVelocity.normalized, intoWall) >= SlideWhenFacingWall)
        {
            Vector2 alongWall = desiredVelocity - intoWall * Vector2.Dot(desiredVelocity, intoWall);
            if (alongWall.sqrMagnitude < SteeringMath.Epsilon)
                alongWall = Vector2.Perpendicular(awayFromWall);

            return alongWall.normalized * maxAvoidForce;
        }

        return awayFromWall * maxAvoidForce;
    }

    float BodyRadius() =>
        body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
}
