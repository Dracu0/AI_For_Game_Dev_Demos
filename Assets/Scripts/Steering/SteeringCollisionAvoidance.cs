// ReSharper disable CheckNamespace
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Tuts+ collision avoidance with Unity ray/circle casts along the movement line.
/// https://code.tutsplus.com/understanding-steering-behaviors-collision-avoidance--gamedev-7777t
///
/// CircleCast = a thick ray matching the agent radius — hits collider edges at the right distance.
/// Avoidance force ≈ normalize(ahead − hitPoint) × maxAvoidForce (falls back to hit normal).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    const float MinDistance = 0.0001f;
    const float MinLookAheadScale = 0.5f;

    static readonly Collider2D[] TouchingWalls = new Collider2D[8];

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

    void Awake() => body = GetComponent<CircleCollider2D>();

    public Vector2 GetAvoidanceForce(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed)
    {
        if (!TryGetLookRay(velocity, desiredVelocity, maxSpeed, out Vector2 direction, out float lookLength))
            return Vector2.zero;

        float castRadius = Radius() + obstaclePadding;
        if (!TryFindClosestHit(position, direction, lookLength, castRadius, out CastHit hit))
            return Vector2.zero;

        Vector2 ahead = position + direction * lookLength;
        return BuildForce(position, ahead, hit, desiredVelocity);
    }

    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < MinDistance)
            return velocity;

        ContactFilter2D filter = default;
        filter.SetLayerMask(obstacleLayers);
        filter.useTriggers = false;

        int count = Physics2D.OverlapCircle(position, Radius(), filter, TouchingWalls);
        Vector2 result = velocity;

        for (int i = 0; i < count; i++)
        {
            Collider2D wall = TouchingWalls[i];
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

    bool TryGetLookRay(
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed,
        out Vector2 direction,
        out float lookLength)
    {
        bool wantsToMove = desiredVelocity.sqrMagnitude > MinDistance;
        direction = velocity.sqrMagnitude > MinDistance ? velocity : desiredVelocity;
        if (direction.sqrMagnitude < MinDistance)
        {
            lookLength = 0f;
            return false;
        }

        direction.Normalize();

        float speedScale = maxSpeed > MinDistance
            ? Mathf.Clamp01(velocity.magnitude / maxSpeed)
            : 0f;
        lookLength = maxSeeAhead * speedScale;

        if (wantsToMove && lookLength < maxSeeAhead * MinLookAheadScale)
            lookLength = maxSeeAhead * MinLookAheadScale;

        return true;
    }

    struct CastHit
    {
        public Vector2 Point;
        public Vector2 Normal;
        public float Distance;
    }

    /// <summary>Full and half-length circle casts, plus overlap if already touching a wall.</summary>
    bool TryFindClosestHit(
        Vector2 position,
        Vector2 direction,
        float lookLength,
        float castRadius,
        out CastHit closest)
    {
        closest = default;
        float closestDistance = float.MaxValue;

        RegisterCast(
            Physics2D.CircleCast(position, castRadius, direction, lookLength, obstacleLayers),
            ref closest,
            ref closestDistance);
        RegisterCast(
            Physics2D.CircleCast(position, castRadius, direction, lookLength * 0.5f, obstacleLayers),
            ref closest,
            ref closestDistance);

        ContactFilter2D filter = default;
        filter.SetLayerMask(obstacleLayers);
        filter.useTriggers = false;

        int count = Physics2D.OverlapCircle(position, castRadius, filter, TouchingWalls);
        for (int i = 0; i < count; i++)
        {
            Collider2D wall = TouchingWalls[i];
            if (wall == null || wall == body)
                continue;

            RegisterTouch(position, wall, ref closest, ref closestDistance);
        }

        return closestDistance < float.MaxValue;
    }

    void RegisterCast(RaycastHit2D hit, ref CastHit closest, ref float closestDistance)
    {
        if (hit.collider == null || hit.collider == body || hit.distance >= closestDistance)
            return;

        closestDistance = hit.distance;
        closest = new CastHit
        {
            Point = hit.point,
            Normal = hit.normal,
            Distance = hit.distance
        };
    }

    static void RegisterTouch(
        Vector2 agentPosition,
        Collider2D wall,
        ref CastHit closest,
        ref float closestDistance)
    {
        if (closestDistance <= 0f)
            return;

        Vector2 onSurface = wall.ClosestPoint(agentPosition);
        Vector2 toAgent = agentPosition - onSurface;
        Vector2 normal = toAgent.sqrMagnitude > MinDistance
            ? toAgent.normalized
            : Vector2.up;

        closestDistance = 0f;
        closest = new CastHit
        {
            Point = onSurface,
            Normal = normal,
            Distance = 0f
        };
    }

    Vector2 BuildForce(
        Vector2 position,
        Vector2 ahead,
        CastHit hit,
        Vector2 desiredVelocity)
    {
        Vector2 awayFromWall = ahead - hit.Point;
        if (awayFromWall.sqrMagnitude < MinDistance)
            awayFromWall = hit.Normal.sqrMagnitude > MinDistance ? hit.Normal : position - hit.Point;

        if (awayFromWall.sqrMagnitude < MinDistance)
            return Vector2.zero;

        Vector2 wallOut = awayFromWall.normalized;

        if (desiredVelocity.sqrMagnitude < MinDistance)
            return wallOut * maxAvoidForce;

        Vector2 intoWall = -wallOut;
        float towardWall = Vector2.Dot(desiredVelocity.normalized, intoWall);
        if (towardWall < 0.7f)
            return wallOut * maxAvoidForce;

        Vector2 alongWall = desiredVelocity - intoWall * Vector2.Dot(desiredVelocity, intoWall);
        if (alongWall.sqrMagnitude < MinDistance)
            alongWall = new Vector2(-wallOut.y, wallOut.x);

        return alongWall.normalized * maxAvoidForce;
    }

    float Radius() =>
        body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
}
