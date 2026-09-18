// ReSharper disable CheckNamespace
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Tuts+ collision avoidance using circle casts along the movement line.
/// https://code.tutsplus.com/understanding-steering-behaviors-collision-avoidance--gamedev-7777t
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    const float MinLookAheadScale = 0.5f;

    static readonly Collider2D[] OverlapBuffer = new Collider2D[8];

    [FormerlySerializedAs("obstacleLayers")]
    [FormerlySerializedAs("_obstacleLayers")]
    [SerializeField] LayerMask obstacleLayers = 1 << 3;

    [FormerlySerializedAs("seeAhead")]
    [FormerlySerializedAs("_seeAhead")]
    [SerializeField] float maxSeeAhead = 3f;

    [FormerlySerializedAs("maxAvoidForce")]
    [FormerlySerializedAs("_maxAvoidForce")]
    [SerializeField] float maxAvoidForce = 12f;

    [Tooltip("Extra radius on casts so agents keep a small gap from edges.")]
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

    public Vector2 GetAvoidanceForce(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed)
    {
        if (!TryGetLookRay(velocity, desiredVelocity, maxSpeed, out Vector2 direction, out float lookLength))
            return Vector2.zero;

        float castRadius = Radius() + obstaclePadding;
        if (!TryFindClosestHit(position, direction, lookLength, castRadius, out SurfaceHit hit))
            return Vector2.zero;

        Vector2 ahead = position + direction * lookLength;
        return BuildForce(position, ahead, hit, desiredVelocity);
    }

    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < SteeringMath.Epsilon)
            return velocity;

        int count = Physics2D.OverlapCircle(position, Radius(), obstacleFilter, OverlapBuffer);
        Vector2 result = velocity;

        for (int i = 0; i < count; i++)
        {
            Collider2D wall = OverlapBuffer[i];
            if (wall == null || wall == body)
                continue;

            Vector2 toAgent = position - wall.ClosestPoint(position);
            if (toAgent.sqrMagnitude < SteeringMath.Epsilon)
                continue;

            Vector2 normal = toAgent.normalized;
            float intoWall = Vector2.Dot(result, -normal);
            if (intoWall > 0f)
                result += normal * intoWall;
        }

        return result;
    }

    bool TryGetLookRay(
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed,
        out Vector2 direction,
        out float lookLength)
    {
        bool wantsToMove = desiredVelocity.sqrMagnitude > SteeringMath.Epsilon;
        direction = velocity.sqrMagnitude > SteeringMath.Epsilon ? velocity : desiredVelocity;
        if (direction.sqrMagnitude < SteeringMath.Epsilon)
        {
            lookLength = 0f;
            return false;
        }

        direction.Normalize();

        float speedScale = maxSpeed > SteeringMath.Epsilon
            ? Mathf.Clamp01(velocity.magnitude / maxSpeed)
            : 0f;
        lookLength = maxSeeAhead * speedScale;

        if (wantsToMove && lookLength < maxSeeAhead * MinLookAheadScale)
            lookLength = maxSeeAhead * MinLookAheadScale;

        return true;
    }

    struct SurfaceHit
    {
        public Vector2 Point;
        public Vector2 Normal;
    }

    bool TryFindClosestHit(
        Vector2 position,
        Vector2 direction,
        float lookLength,
        float castRadius,
        out SurfaceHit closest)
    {
        closest = default;
        float closestDistance = float.MaxValue;

        ConsiderCast(
            Physics2D.CircleCast(position, castRadius, direction, lookLength, obstacleLayers),
            ref closest,
            ref closestDistance);
        ConsiderCast(
            Physics2D.CircleCast(position, castRadius, direction, lookLength * 0.5f, obstacleLayers),
            ref closest,
            ref closestDistance);

        int count = Physics2D.OverlapCircle(position, castRadius, obstacleFilter, OverlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D wall = OverlapBuffer[i];
            if (wall == null || wall == body)
                continue;

            ConsiderTouch(position, wall, ref closest, ref closestDistance);
        }

        return closestDistance < float.MaxValue;
    }

    void ConsiderCast(RaycastHit2D hit, ref SurfaceHit closest, ref float closestDistance)
    {
        if (hit.collider == null || hit.collider == body || hit.distance >= closestDistance)
            return;

        closestDistance = hit.distance;
        closest = new SurfaceHit { Point = hit.point, Normal = hit.normal };
    }

    static void ConsiderTouch(
        Vector2 agentPosition,
        Collider2D wall,
        ref SurfaceHit closest,
        ref float closestDistance)
    {
        if (closestDistance <= 0f)
            return;

        Vector2 onSurface = wall.ClosestPoint(agentPosition);
        Vector2 toAgent = agentPosition - onSurface;
        Vector2 normal = toAgent.sqrMagnitude > SteeringMath.Epsilon
            ? toAgent.normalized
            : Vector2.up;

        closestDistance = 0f;
        closest = new SurfaceHit { Point = onSurface, Normal = normal };
    }

    Vector2 BuildForce(
        Vector2 position,
        Vector2 ahead,
        SurfaceHit hit,
        Vector2 desiredVelocity)
    {
        Vector2 awayFromWall = ahead - hit.Point;
        if (awayFromWall.sqrMagnitude < SteeringMath.Epsilon)
            awayFromWall = hit.Normal.sqrMagnitude > SteeringMath.Epsilon ? hit.Normal : position - hit.Point;

        if (awayFromWall.sqrMagnitude < SteeringMath.Epsilon)
            return Vector2.zero;

        Vector2 wallOut = awayFromWall.normalized;

        if (desiredVelocity.sqrMagnitude < SteeringMath.Epsilon)
            return wallOut * maxAvoidForce;

        Vector2 intoWall = -wallOut;
        if (Vector2.Dot(desiredVelocity.normalized, intoWall) < 0.7f)
            return wallOut * maxAvoidForce;

        Vector2 alongWall = desiredVelocity - intoWall * Vector2.Dot(desiredVelocity, intoWall);
        if (alongWall.sqrMagnitude < SteeringMath.Epsilon)
            alongWall = new Vector2(-wallOut.y, wallOut.x);

        return alongWall.normalized * maxAvoidForce;
    }

    float Radius() =>
        body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
}
