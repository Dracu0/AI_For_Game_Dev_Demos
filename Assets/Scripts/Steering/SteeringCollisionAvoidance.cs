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
    [SerializeField] bool showGizmos = true;

    CircleCollider2D body;

    // Last FixedUpdate sample (for Scene-view gizmos).
    bool gizmoHasSample;
    Vector2 gizmoPosition;
    Vector2 gizmoDirection;
    float gizmoLookDistance;
    bool gizmoFoundWall;
    Vector2 gizmoWallNormal;
    Vector2 gizmoWallContact;
    Vector2 gizmoForce;

    void Awake() => body = GetComponent<CircleCollider2D>();

    // -------------------------------------------------------------------------
    // Called from SteeringMath.Steer
    // -------------------------------------------------------------------------

    public Vector2 GetAvoidanceForce(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed)
    {
        Vector2 lookDirection = PickLookDirection(velocity, desiredVelocity);
        float lookDistance = LookAheadDistance(velocity, desiredVelocity, maxSpeed);

        Vector2 force = Vector2.zero;
        bool foundWall = false;
        Vector2 wallNormal = default;
        Vector2 wallContact = default;

        if (lookDirection.sqrMagnitude >= SteeringMath.Epsilon
            && TryFindWall(position, lookDirection, lookDistance, out wallNormal, out wallContact))
        {
            foundWall = true;
            force = ComputeAvoidForce(wallNormal, desiredVelocity);
        }

        StoreGizmoSample(position, lookDirection, lookDistance, foundWall, wallNormal, wallContact, force);
        return force;
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

    // -------------------------------------------------------------------------
    // Look-ahead
    // -------------------------------------------------------------------------

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
        out Vector2 wallNormal,
        out Vector2 wallContact)
    {
        float radius = KeepOutRadius();

        // CircleCast starts outside the body, so it misses a wall we are already touching.
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, radius, obstacleLayers);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == body)
                continue;

            wallContact = wall.ClosestPoint(position);
            Vector2 away = position - wallContact;
            wallNormal = away.sqrMagnitude > SteeringMath.Epsilon ? away.normalized : Vector2.up;
            return true;
        }

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

    Vector2 ComputeAvoidForce(Vector2 wallNormal, Vector2 desiredVelocity)
    {
        Vector2 pushOut = wallNormal * maxAvoidForce;

        if (desiredVelocity.sqrMagnitude < SteeringMath.Epsilon)
            return pushOut;

        Vector2 intoWall = -wallNormal;
        if (Vector2.Dot(desiredVelocity.normalized, intoWall) < SlideWhenFacingWall)
            return pushOut;

        Vector2 alongWall = desiredVelocity - intoWall * Vector2.Dot(desiredVelocity, intoWall);
        if (alongWall.sqrMagnitude < SteeringMath.Epsilon)
            alongWall = Vector2.Perpendicular(wallNormal);

        return alongWall.normalized * maxAvoidForce + pushOut;
    }

    // -------------------------------------------------------------------------
    // Geometry & wall helpers
    // -------------------------------------------------------------------------

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
    // Scene view: yellow = look-ahead, green = wall normal, magenta = force, red = keep-out
    // -------------------------------------------------------------------------

    void StoreGizmoSample(
        Vector2 position,
        Vector2 lookDirection,
        float lookDistance,
        bool foundWall,
        Vector2 wallNormal,
        Vector2 wallContact,
        Vector2 force)
    {
        gizmoHasSample = true;
        gizmoPosition = position;
        gizmoDirection = lookDirection;
        gizmoLookDistance = lookDistance;
        gizmoFoundWall = foundWall;
        gizmoWallNormal = wallNormal;
        gizmoWallContact = wallContact;
        gizmoForce = force;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Vector2 origin = gizmoHasSample ? gizmoPosition : (Vector2)transform.position;
        float radius = KeepOutRadius();

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
        DrawCircle(origin, radius);

        if (!gizmoHasSample || gizmoDirection.sqrMagnitude < SteeringMath.Epsilon)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
            DrawCircle(origin, maxSeeAhead);
            DrawKeepOutNormals(origin);
            return;
        }

        Vector2 ahead = origin + gizmoDirection * gizmoLookDistance;
        Gizmos.DrawLine(origin, ahead);
        DrawCircle(ahead, radius);

        if (gizmoFoundWall)
        {
            Gizmos.color = new Color(0.2f, 0.95f, 0.35f);
            DrawArrow(gizmoWallContact, gizmoWallNormal);
        }

        Gizmos.color = new Color(0.9f, 0.35f, 1f);
        if (gizmoForce.sqrMagnitude > SteeringMath.Epsilon)
            DrawArrow(origin, gizmoForce.normalized * 1.5f);

        DrawKeepOutNormals(origin);
    }

    void DrawKeepOutNormals(Vector2 origin)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(origin, KeepOutRadius(), obstacleLayers);
        Gizmos.color = new Color(1f, 0.3f, 0.3f);

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (!TryGetOutOfWall(origin, overlaps[i], out Vector2 outOfWall))
                continue;

            Vector2 closest = overlaps[i].ClosestPoint(origin);
            DrawArrow(closest, outOfWall * 0.75f);
        }
    }

    static void DrawArrow(Vector2 from, Vector2 vector)
    {
        if (vector.sqrMagnitude < SteeringMath.Epsilon)
            return;

        Vector2 tip = from + vector;
        Gizmos.DrawLine(from, tip);

        Vector2 direction = vector.normalized;
        float head = Mathf.Min(0.2f, vector.magnitude * 0.3f);
        Vector2 wing = Vector2.Perpendicular(direction) * head;
        Vector2 back = tip - direction * head;
        Gizmos.DrawLine(tip, back + wing);
        Gizmos.DrawLine(tip, back - wing);
    }

    static void DrawCircle(Vector2 center, float radius)
    {
        if (radius <= 0f)
            return;

        const int segments = 24;
        Vector2 previous = center + Vector2.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
