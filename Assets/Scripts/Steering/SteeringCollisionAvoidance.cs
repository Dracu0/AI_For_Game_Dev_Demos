using UnityEngine;

/// <summary>
/// Look-ahead obstacle avoidance.
///
/// 1. Cast a circle along the movement line.
/// 2. If a wall is found, steer away from it (or slide along it).
/// 3. If already overlapping a wall, cancel velocity that would push deeper.
///
/// https://code.tutsplus.com/understanding-steering-behaviors-collision-avoidance--gamedev-7777t
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    // Cosine of ~45°. If the desired heading is more into the wall than this,
    // slide along the wall instead of bouncing off it.
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
        Vector2 lookDirection = velocity.sqrMagnitude > SteeringMath.Epsilon
            ? velocity
            : desiredVelocity;

        if (lookDirection.sqrMagnitude < SteeringMath.Epsilon)
            return Vector2.zero;

        lookDirection.Normalize();

        float lookLength = LookAheadLength(velocity, desiredVelocity, maxSpeed);
        float radius = Radius() + obstaclePadding;

        if (!TryFindWall(position, lookDirection, lookLength, radius, out Vector2 wallPoint, out Vector2 wallNormal))
            return Vector2.zero;

        Vector2 ahead = position + lookDirection * lookLength;
        return AvoidForce(ahead, wallPoint, wallNormal, desiredVelocity);
    }

    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < SteeringMath.Epsilon)
            return velocity;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, Radius(), obstacleLayers);
        Vector2 result = velocity;

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == body)
                continue;

            Vector2 away = position - wall.ClosestPoint(position);
            if (away.sqrMagnitude < SteeringMath.Epsilon)
                continue;

            Vector2 normal = away.normalized;
            float intoWall = Vector2.Dot(result, -normal);
            if (intoWall > 0f)
                result += normal * intoWall;
        }

        return result;
    }

    float LookAheadLength(Vector2 velocity, Vector2 desiredVelocity, float maxSpeed)
    {
        float speedScale = maxSpeed > SteeringMath.Epsilon
            ? Mathf.Clamp01(velocity.magnitude / maxSpeed)
            : 0f;

        float lookLength = maxSeeAhead * speedScale;

        // Keep a minimum look-ahead while the agent intends to move,
        // otherwise it only sees walls after it is already going fast.
        bool wantsToMove = desiredVelocity.sqrMagnitude > SteeringMath.Epsilon;
        if (wantsToMove)
            lookLength = Mathf.Max(lookLength, maxSeeAhead * 0.5f);

        return lookLength;
    }

    bool TryFindWall(
        Vector2 position,
        Vector2 direction,
        float lookLength,
        float radius,
        out Vector2 point,
        out Vector2 normal)
    {
        point = default;
        normal = default;

        // CircleCast can miss a wall we are already overlapping, so check that first.
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, radius, obstacleLayers);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == body)
                continue;

            point = wall.ClosestPoint(position);
            Vector2 away = position - point;
            normal = away.sqrMagnitude > SteeringMath.Epsilon ? away.normalized : Vector2.up;
            return true;
        }

        RaycastHit2D hit = Physics2D.CircleCast(position, radius, direction, lookLength, obstacleLayers);
        if (hit.collider == null || hit.collider == body)
            return false;

        point = hit.point;
        normal = hit.normal;
        return true;
    }

    Vector2 AvoidForce(Vector2 ahead, Vector2 wallPoint, Vector2 wallNormal, Vector2 desiredVelocity)
    {
        Vector2 away = ahead - wallPoint;
        if (away.sqrMagnitude < SteeringMath.Epsilon)
            away = wallNormal;

        if (away.sqrMagnitude < SteeringMath.Epsilon)
            return Vector2.zero;

        Vector2 awayFromWall = away.normalized;

        if (desiredVelocity.sqrMagnitude < SteeringMath.Epsilon)
            return awayFromWall * maxAvoidForce;

        Vector2 intoWall = -awayFromWall;
        if (Vector2.Dot(desiredVelocity.normalized, intoWall) < SlideWhenFacingWall)
            return awayFromWall * maxAvoidForce;

        Vector2 alongWall = desiredVelocity - intoWall * Vector2.Dot(desiredVelocity, intoWall);
        if (alongWall.sqrMagnitude < SteeringMath.Epsilon)
            alongWall = new Vector2(-awayFromWall.y, awayFromWall.x);

        return alongWall.normalized * maxAvoidForce;
    }

    float Radius() =>
        body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
}
