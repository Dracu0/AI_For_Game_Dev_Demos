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
        Vector2 awayFromWall = Vector2.zero;
        Vector2 force = Vector2.zero;
        float lookDistance = 0f;
        bool foundWall = false;

        if (direction.sqrMagnitude >= SteeringMath.Epsilon)
        {
            direction.Normalize();
            lookDistance = LookAheadDistance(velocity, desiredVelocity, maxSpeed);
            foundWall = TryFindWall(position, direction, lookDistance, out awayFromWall);
            if (foundWall)
                force = AvoidForce(awayFromWall, desiredVelocity);
        }

        Remember(position, direction, lookDistance, foundWall, awayFromWall, force);
        return force;
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

            Vector2 closest = wall.ClosestPoint(position);
            RememberContact(closest);
            Vector2 away = position - closest;
            awayFromWall = away.sqrMagnitude > SteeringMath.Epsilon ? away.normalized : Vector2.up;
            return true;
        }

        RaycastHit2D hit = Physics2D.CircleCast(position, radius, direction, lookDistance, obstacleLayers);
        if (hit.collider == null || hit.collider == body || hit.normal.sqrMagnitude < SteeringMath.Epsilon)
        {
            RememberContact(default, false);
            awayFromWall = default;
            return false;
        }

        RememberContact(hit.point);
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

    float BodyRadius()
    {
        if (body == null)
            body = GetComponent<CircleCollider2D>();

        if (body == null)
            return 0f;

        return body.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
    }

    // Scene view: yellow = look-ahead, green = wall normal, magenta = steer force, red = overlap push-out.
    [SerializeField] bool showGizmos = true;

    bool gizmoHasSample;
    Vector2 gizmoPosition;
    Vector2 gizmoDirection;
    float gizmoLookDistance;
    bool gizmoFoundWall;
    Vector2 gizmoAway;
    Vector2 gizmoForce;
    bool gizmoHasContact;
    Vector2 gizmoContact;

    void Remember(
        Vector2 position,
        Vector2 direction,
        float lookDistance,
        bool foundWall,
        Vector2 awayFromWall,
        Vector2 force)
    {
        gizmoHasSample = true;
        gizmoPosition = position;
        gizmoDirection = direction;
        gizmoLookDistance = lookDistance;
        gizmoFoundWall = foundWall;
        gizmoAway = awayFromWall;
        gizmoForce = force;
        if (!foundWall)
            gizmoHasContact = false;
    }

    void RememberContact(Vector2 contact, bool hasContact = true)
    {
        gizmoHasContact = hasContact;
        gizmoContact = contact;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        float radius = BodyRadius() + obstaclePadding;
        Vector2 origin = gizmoHasSample ? gizmoPosition : (Vector2)transform.position;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
        DrawCircle(origin, radius);

        if (!gizmoHasSample || gizmoDirection.sqrMagnitude < SteeringMath.Epsilon)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
            DrawCircle(origin, maxSeeAhead);
            DrawOverlapNormals(origin);
            return;
        }

        Vector2 ahead = origin + gizmoDirection * gizmoLookDistance;
        Gizmos.DrawLine(origin, ahead);
        DrawCircle(ahead, radius);

        if (gizmoFoundWall && gizmoHasContact)
        {
            Gizmos.color = new Color(0.2f, 0.95f, 0.35f);
            DrawArrow(gizmoContact, gizmoAway);
        }

        Gizmos.color = new Color(0.9f, 0.35f, 1f);
        if (gizmoForce.sqrMagnitude > SteeringMath.Epsilon)
            DrawArrow(origin, gizmoForce.normalized * 1.5f);

        DrawOverlapNormals(origin);
    }

    void DrawOverlapNormals(Vector2 origin)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(origin, BodyRadius(), obstacleLayers);
        Gizmos.color = new Color(1f, 0.3f, 0.3f);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == body)
                continue;

            Vector2 closest = wall.ClosestPoint(origin);
            Vector2 outOfWall = origin - closest;
            if (outOfWall.sqrMagnitude < SteeringMath.Epsilon)
                continue;

            DrawArrow(closest, outOfWall.normalized * 0.75f);
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
