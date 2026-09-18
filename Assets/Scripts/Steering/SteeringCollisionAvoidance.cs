// ReSharper disable CheckNamespace
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Collision avoidance from Fernando Bevilacqua's Tuts+ steering series:
/// https://code.tutsplus.com/understanding-steering-behaviors-collision-avoidance--gamedev-7777t
///
/// 1. Cast two "look ahead" points along velocity (shorter when moving slowly).
/// 2. If position, ahead, or ahead/2 hits an obstacle, pick the closest one.
/// 3. avoidanceForce = normalize(ahead - obstacle) × maxAvoidForce
/// 4. SteeringMath adds this force to seek/flee steering before integrating velocity.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    const float MinDistance = 0.0001f;

    static readonly Collider2D[] NearbyObstacles = new Collider2D[16];

    [FormerlySerializedAs("obstacleLayers")]
    [FormerlySerializedAs("_obstacleLayers")]
    [SerializeField] LayerMask obstacleLayers = 1 << 3;

    [FormerlySerializedAs("seeAhead")]
    [FormerlySerializedAs("_seeAhead")]
    [SerializeField] float maxSeeAhead = 3f;

    [FormerlySerializedAs("maxAvoidForce")]
    [FormerlySerializedAs("_maxAvoidForce")]
    [SerializeField] float maxAvoidForce = 12f;

    [Tooltip("Treats the agent as slightly larger when testing ahead points.")]
    [FormerlySerializedAs("_wallClearance")]
    [SerializeField] float obstaclePadding = 0.4f;

    CircleCollider2D body;
    ContactFilter2D obstacleFilter;

    void Awake()
    {
        body = GetComponent<CircleCollider2D>();
        obstacleFilter = new ContactFilter2D { useLayerMask = true, useTriggers = false };
        obstacleFilter.SetLayerMask(obstacleLayers);
    }

    /// <summary>Force to add to steering (seek + avoidance, then clamp).</summary>
    public Vector2 GetAvoidanceForce(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed)
    {
        BuildLookAheadPoints(position, velocity, desiredVelocity, maxSpeed, out Vector2 ahead, out Vector2 ahead2);

        float agentRadius = Radius();
        Collider2D obstacle = FindMostThreateningObstacle(position, ahead, ahead2, agentRadius);
        if (obstacle == null)
            return Vector2.zero;

        return ComputeAvoidanceForce(position, ahead, obstacle);
    }

    /// <summary>Stops velocity from pushing the circle deeper into a wall.</summary>
    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < MinDistance)
            return velocity;

        int count = Physics2D.OverlapCircle(position, Radius(), obstacleFilter, NearbyObstacles);
        Vector2 result = velocity;

        for (int i = 0; i < count; i++)
        {
            Collider2D wall = NearbyObstacles[i];
            if (wall == null || wall == body)
                continue;

            Vector2 toAgent = position - wall.ClosestPoint(position);
            if (toAgent.sqrMagnitude < MinDistance)
                continue;

            Vector2 normal = toAgent.normalized;
            float intoWall = Vector2.Dot(result, -normal);
            if (intoWall > 0f)
                result += normal * intoWall;
        }

        return result;
    }

    // --- Tuts+ look ahead (velocity direction, length scales with speed) ---

    void BuildLookAheadPoints(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed,
        out Vector2 ahead,
        out Vector2 ahead2)
    {
        Vector2 direction = velocity.sqrMagnitude > MinDistance ? velocity : desiredVelocity;
        if (direction.sqrMagnitude < MinDistance)
        {
            ahead = position;
            ahead2 = position;
            return;
        }

        direction.Normalize();

        float lookLength = maxSeeAhead;
        if (maxSpeed > MinDistance)
            lookLength *= Mathf.Clamp01(velocity.magnitude / maxSpeed);

        ahead = position + direction * lookLength;
        ahead2 = position + direction * lookLength * 0.5f;
    }

    // --- Tuts+ findMostThreateningObstacle ---

    Collider2D FindMostThreateningObstacle(
        Vector2 position,
        Vector2 ahead,
        Vector2 ahead2,
        float agentRadius)
    {
        float hitRadius = agentRadius + obstaclePadding;
        float searchRadius = Vector2.Distance(position, ahead) + hitRadius;

        int count = Physics2D.OverlapCircle(position, searchRadius, obstacleFilter, NearbyObstacles);
        Collider2D closest = null;
        float closestDistanceSq = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider2D candidate = NearbyObstacles[i];
            if (candidate == null || candidate == body)
                continue;

            if (!ObstacleBlocksPath(position, ahead, ahead2, hitRadius, candidate))
                continue;

            float distanceSq = (candidate.ClosestPoint(position) - position).sqrMagnitude;
            if (distanceSq >= closestDistanceSq)
                continue;

            closestDistanceSq = distanceSq;
            closest = candidate;
        }

        return closest;
    }

    static bool ObstacleBlocksPath(
        Vector2 position,
        Vector2 ahead,
        Vector2 ahead2,
        float hitRadius,
        Collider2D obstacle)
    {
        return PointHitsObstacle(position, hitRadius, obstacle)
            || PointHitsObstacle(ahead, hitRadius, obstacle)
            || PointHitsObstacle(ahead2, hitRadius, obstacle);
    }

    static bool PointHitsObstacle(Vector2 point, float hitRadius, Collider2D obstacle)
    {
        Vector2 onSurface = obstacle.ClosestPoint(point);
        return (point - onSurface).sqrMagnitude <= hitRadius * hitRadius;
    }

    Vector2 ComputeAvoidanceForce(Vector2 position, Vector2 ahead, Collider2D obstacle)
    {
        Vector2 push = ahead - obstacle.ClosestPoint(ahead);
        if (push.sqrMagnitude < MinDistance)
            push = position - obstacle.ClosestPoint(position);

        if (push.sqrMagnitude < MinDistance)
            return Vector2.zero;

        return push.normalized * maxAvoidForce;
    }

    float Radius() =>
        body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
}
