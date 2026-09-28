using UnityEngine;

/// <summary>
/// Look-ahead obstacle avoidance.
///
/// Cast a circle along the movement line. If a wall is in the way, steer away
/// from it — or slide along it when heading into it. If the agent is inside the
/// padded gap, cancel the velocity that would push deeper.
///
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    const float SlideWhenFacingWall = 0.7f; // cos(45°): head-on enough to slide along the wall

    [SerializeField] LayerMask obstacleLayers = 1 << 3;
    [SerializeField] float maxSeeAhead = 3f;
    [SerializeField] float maxAvoidForce = 12f;
    [Tooltip("Extra radius so agents keep a small gap from walls.")]
    [SerializeField] float obstaclePadding = 0.4f;

    CircleCollider2D body;
    Vector2 _committedWorldTangent;

    /// <summary>
    /// If the goal is blocked head-on, replaces desired with one slide direction.
    /// Otherwise returns desired and an optional push-out in <paramref name="extraForce"/>.
    /// </summary>
    public Vector2 ResolveDesired(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed,
        out Vector2 extraForce)
    {
        extraForce = Vector2.zero;

        Vector2 lookDirection = PickLookDirection(velocity, desiredVelocity);
        float lookDistance = LookAheadDistance(velocity, desiredVelocity, maxSpeed);
        Vector2 wallNormal = default;
        Vector2 wallContact = default;
        Vector2 resolved = desiredVelocity;

        if (desiredVelocity.sqrMagnitude > SteeringMath.Epsilon)
        {
            Vector2 goalDir = desiredVelocity.normalized;
            bool goalBlocked = !IsGoalClear(position, goalDir, lookDistance);
            float speed = desiredVelocity.magnitude;

            if (goalBlocked
                && TryFindWall(position, goalDir, lookDistance, desiredVelocity, out wallNormal, out wallContact))
            {
                bool facingWall = Vector2.Dot(goalDir, -wallNormal) >= SlideWhenFacingWall;
                if (facingWall || _committedWorldTangent.sqrMagnitude > SteeringMath.Epsilon)
                {
                    resolved = GetCommittedTangent(wallNormal, goalDir) * speed;
                    StoreGizmoSample(position, goalDir, lookDistance, resolved);
                    return resolved;
                }
            }
            else if (goalBlocked && _committedWorldTangent.sqrMagnitude > SteeringMath.Epsilon)
            {
                resolved = _committedWorldTangent * speed;
                StoreGizmoSample(position, goalDir, lookDistance, resolved);
                return resolved;
            }

            if (!goalBlocked)
                _committedWorldTangent = Vector2.zero;
        }

        if (lookDirection.sqrMagnitude >= SteeringMath.Epsilon
            && TryFindWall(position, lookDirection, lookDistance, desiredVelocity, out wallNormal, out wallContact))
        {
            extraForce = wallNormal * maxAvoidForce;
        }

        StoreGizmoSample(position, lookDirection, lookDistance, resolved);
        return resolved;
    }

    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < SteeringMath.Epsilon)
            return velocity;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, KeepOutRadius(), obstacleLayers);
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (!TryGetOutOfWall(position, overlaps[i], out Vector2 outOfWall))
                continue;

            float pushingIn = Vector2.Dot(velocity, -outOfWall);
            if (pushingIn > 0f)
                velocity += outOfWall * pushingIn;
        }

        return velocity;
    }

    void Awake() => body = GetComponent<CircleCollider2D>();

    static Vector2 PickLookDirection(Vector2 velocity, Vector2 desiredVelocity)
    {
        Vector2 direction = velocity.sqrMagnitude > SteeringMath.Epsilon ? velocity : desiredVelocity;
        if (direction.sqrMagnitude < SteeringMath.Epsilon)
            return Vector2.zero;

        return direction.normalized;
    }

    float LookAheadDistance(Vector2 velocity, Vector2 desiredVelocity, float maxSpeed)
    {
        float distance = maxSpeed > SteeringMath.Epsilon
            ? maxSeeAhead * Mathf.Clamp01(velocity.magnitude / maxSpeed)
            : 0f;

        if (desiredVelocity.sqrMagnitude > SteeringMath.Epsilon)
            distance = Mathf.Max(distance, maxSeeAhead * 0.5f);

        return distance;
    }

    bool TryFindWall(
        Vector2 position,
        Vector2 direction,
        float lookDistance,
        Vector2 desiredVelocity,
        out Vector2 wallNormal,
        out Vector2 wallContact)
    {
        wallNormal = default;
        wallContact = default;

        float radius = KeepOutRadius();
        Vector2 goalDir = desiredVelocity.sqrMagnitude > SteeringMath.Epsilon
            ? desiredVelocity.normalized
            : direction;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, radius, obstacleLayers);
        float bestBlock = -1f;
        bool foundOverlap = false;
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == body)
                continue;

            Vector2 contact = wall.ClosestPoint(position);
            Vector2 away = position - contact;
            if (away.sqrMagnitude < SteeringMath.Epsilon)
                continue;

            Vector2 normal = away.normalized;
            float block = Vector2.Dot(goalDir, -normal);
            if (block <= bestBlock)
                continue;

            bestBlock = block;
            wallContact = contact;
            wallNormal = normal;
            foundOverlap = true;
        }

        if (foundOverlap)
            return true;

        RaycastHit2D hit = Physics2D.CircleCast(position, radius, direction, lookDistance, obstacleLayers);
        if (hit.collider == null || hit.collider == body || hit.normal.sqrMagnitude < SteeringMath.Epsilon)
        {
            wallNormal = default;
            wallContact = default;
            return false;
        }

        wallNormal = hit.normal.normalized;
        wallContact = hit.point;
        return true;
    }

    bool IsGoalClear(Vector2 position, Vector2 goalDir, float lookDistance)
    {
        if (goalDir.sqrMagnitude < SteeringMath.Epsilon || lookDistance <= 0f)
            return true;

        float radius = KeepOutRadius();
        RaycastHit2D hit = Physics2D.CircleCast(position, radius, goalDir, lookDistance, obstacleLayers);
        return hit.collider == null || hit.collider == body;
    }

    Vector2 GetCommittedTangent(Vector2 wallNormal, Vector2 goalDir)
    {
        Vector2 tangent = new Vector2(-wallNormal.y, wallNormal.x);
        if (_committedWorldTangent.sqrMagnitude >= SteeringMath.Epsilon)
        {
            if (Vector2.Dot(tangent, _committedWorldTangent) < 0f)
                tangent = -tangent;
        }
        else
            tangent *= PickSideSign(wallNormal, goalDir);

        _committedWorldTangent = tangent.normalized;
        return _committedWorldTangent;
    }

    int PickSideSign(Vector2 wallNormal, Vector2 goalDir)
    {
        float cross = wallNormal.x * goalDir.y - wallNormal.y * goalDir.x;
        if (Mathf.Abs(cross) > 0.05f)
            return cross > 0f ? 1 : -1;

        Vector2 tangent = new Vector2(-wallNormal.y, wallNormal.x);
        float along = Vector2.Dot(tangent, goalDir);
        if (Mathf.Abs(along) > 0.05f)
            return along >= 0f ? 1 : -1;

        return (GetInstanceID() & 1) == 0 ? 1 : -1;
    }

    float BodyRadius()
    {
        if (body == null)
            body = GetComponent<CircleCollider2D>();

        if (body == null)
            return 0f;

        return body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
    }

    float KeepOutRadius() => BodyRadius() + Mathf.Max(0f, obstaclePadding);

    bool TryGetOutOfWall(Vector2 position, Collider2D wall, out Vector2 outOfWall)
    {
        outOfWall = default;
        if (wall == null || wall == body)
            return false;

        Vector2 away = position - wall.ClosestPoint(position);
        if (away.sqrMagnitude < SteeringMath.Epsilon)
            return false;

        outOfWall = away.normalized;
        return true;
    }

    // -------------------------------------------------------------------------
    // Gizmos — yellow = circle cast, magenta = velocity actually used
    // -------------------------------------------------------------------------

    [SerializeField] bool showGizmos = true;

    bool gizmoHasSample;
    Vector2 gizmoPosition;
    Vector2 gizmoDirection;
    float gizmoLookDistance;
    Vector2 gizmoVelocity;

    void StoreGizmoSample(Vector2 position, Vector2 lookDirection, float lookDistance, Vector2 velocity)
    {
        gizmoHasSample = true;
        gizmoPosition = position;
        gizmoDirection = lookDirection;
        gizmoLookDistance = lookDistance;
        gizmoVelocity = velocity;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Vector2 origin = gizmoHasSample ? gizmoPosition : (Vector2)transform.position;
        Vector2 direction = gizmoHasSample ? gizmoDirection : (Vector2)transform.right;
        float lookDistance = gizmoHasSample ? gizmoLookDistance : maxSeeAhead;
        float radius = KeepOutRadius();

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(origin, radius);

        if (direction.sqrMagnitude > SteeringMath.Epsilon && lookDistance > 0f)
        {
            Vector2 ahead = origin + direction.normalized * lookDistance;
            Gizmos.DrawLine(origin, ahead);
            Gizmos.DrawWireSphere(ahead, radius);
        }

        if (gizmoVelocity.sqrMagnitude <= SteeringMath.Epsilon)
            return;

        Gizmos.color = new Color(0.9f, 0.35f, 1f, 0.95f);
        Gizmos.DrawLine(origin, origin + gizmoVelocity);
    }
}
