using UnityEngine;

/// <summary>
/// Looks ahead for a wall. Head-on, it commits to one side and slides.
/// Otherwise it pushes away from the wall.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    const float HeadOn = 0.7f;

    [SerializeField] LayerMask obstacleLayers = 1 << 3;
    [SerializeField] float maxSeeAhead = 3f;
    [SerializeField] float maxAvoidForce = 12f;
    [SerializeField] float obstaclePadding = 0.4f;

    CircleCollider2D _body;
    Vector2 _slideDirection;

    public Vector2 ResolveDesired(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed,
        out Vector2 extraForce)
    {
        extraForce = Vector2.zero;

        Vector2 lookDirection = LookDirection(velocity, desiredVelocity);
        float lookDistance = LookDistance(velocity, desiredVelocity, maxSpeed);
        Vector2 resolved = desiredVelocity;

        if (desiredVelocity.sqrMagnitude > SteeringMath.Epsilon)
        {
            Vector2 goalDir = desiredVelocity.normalized;
            float speed = desiredVelocity.magnitude;
            bool blocked = !GoalIsClear(position, goalDir, lookDistance);

            if (blocked && TryFindWall(position, goalDir, lookDistance, desiredVelocity, out Vector2 wallNormal))
            {
                bool headOn = Vector2.Dot(goalDir, -wallNormal) >= HeadOn;
                if (headOn || _slideDirection != Vector2.zero)
                {
                    resolved = SlideDirection(wallNormal, goalDir) * speed;
                    Remember(position, goalDir, lookDistance, resolved);
                    return resolved;
                }
            }
            else if (blocked && _slideDirection != Vector2.zero)
            {
                resolved = _slideDirection * speed;
                Remember(position, goalDir, lookDistance, resolved);
                return resolved;
            }

            if (!blocked)
                _slideDirection = Vector2.zero;
        }

        if (lookDirection != Vector2.zero
            && TryFindWall(position, lookDirection, lookDistance, desiredVelocity, out Vector2 normal))
            extraForce = normal * maxAvoidForce;

        Remember(position, lookDirection, lookDistance, resolved);
        return resolved;
    }

    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < SteeringMath.Epsilon)
            return velocity;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, KeepOutRadius(), obstacleLayers);
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (!AwayFromWall(position, overlaps[i], out Vector2 away))
                continue;

            float pushingIn = Vector2.Dot(velocity, -away);
            if (pushingIn > 0f)
                velocity += away * pushingIn;
        }

        return velocity;
    }

    void Awake() => _body = GetComponent<CircleCollider2D>();

    static Vector2 LookDirection(Vector2 velocity, Vector2 desiredVelocity)
    {
        Vector2 direction = velocity.sqrMagnitude > SteeringMath.Epsilon ? velocity : desiredVelocity;
        return direction.sqrMagnitude < SteeringMath.Epsilon ? Vector2.zero : direction.normalized;
    }

    float LookDistance(Vector2 velocity, Vector2 desiredVelocity, float maxSpeed)
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
        out Vector2 wallNormal)
    {
        wallNormal = Vector2.zero;
        float radius = KeepOutRadius();
        Vector2 goalDir = desiredVelocity.sqrMagnitude > SteeringMath.Epsilon
            ? desiredVelocity.normalized
            : direction;

        float bestBlock = -1f;
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, radius, obstacleLayers);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == _body)
                continue;

            Vector2 away = position - wall.ClosestPoint(position);
            if (away.sqrMagnitude < SteeringMath.Epsilon)
                continue;

            Vector2 normal = away.normalized;
            float block = Vector2.Dot(goalDir, -normal);
            if (block <= bestBlock)
                continue;

            bestBlock = block;
            wallNormal = normal;
        }

        if (wallNormal != Vector2.zero)
            return true;

        RaycastHit2D hit = Physics2D.CircleCast(position, radius, direction, lookDistance, obstacleLayers);
        if (hit.collider == null || hit.collider == _body || hit.normal == Vector2.zero)
            return false;

        wallNormal = hit.normal.normalized;
        return true;
    }

    bool GoalIsClear(Vector2 position, Vector2 goalDir, float lookDistance)
    {
        if (goalDir == Vector2.zero || lookDistance <= 0f)
            return true;

        RaycastHit2D hit = Physics2D.CircleCast(position, KeepOutRadius(), goalDir, lookDistance, obstacleLayers);
        return hit.collider == null || hit.collider == _body;
    }

    Vector2 SlideDirection(Vector2 wallNormal, Vector2 goalDir)
    {
        Vector2 tangent = new Vector2(-wallNormal.y, wallNormal.x);
        if (_slideDirection != Vector2.zero)
        {
            if (Vector2.Dot(tangent, _slideDirection) < 0f)
                tangent = -tangent;
        }
        else
            tangent *= SideSign(wallNormal, goalDir);

        _slideDirection = tangent.normalized;
        return _slideDirection;
    }

    int SideSign(Vector2 wallNormal, Vector2 goalDir)
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

    float KeepOutRadius()
    {
        if (_body == null)
            _body = GetComponent<CircleCollider2D>();
        if (_body == null)
            return Mathf.Max(0f, obstaclePadding);

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        return _body.radius * scale + Mathf.Max(0f, obstaclePadding);
    }

    bool AwayFromWall(Vector2 position, Collider2D wall, out Vector2 away)
    {
        away = Vector2.zero;
        if (wall == null || wall == _body)
            return false;

        Vector2 offset = position - wall.ClosestPoint(position);
        if (offset.sqrMagnitude < SteeringMath.Epsilon)
            return false;

        away = offset.normalized;
        return true;
    }

    // -------------------------------------------------------------------------
    // Gizmos — yellow = look-ahead cast, magenta = velocity used
    // -------------------------------------------------------------------------

    [SerializeField] bool showGizmos = true;

    bool _hasSample;
    Vector2 _samplePosition;
    Vector2 _sampleDirection;
    float _sampleDistance;
    Vector2 _sampleVelocity;

    void Remember(Vector2 position, Vector2 direction, float distance, Vector2 velocity)
    {
        _hasSample = true;
        _samplePosition = position;
        _sampleDirection = direction;
        _sampleDistance = distance;
        _sampleVelocity = velocity;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Vector2 origin = _hasSample ? _samplePosition : (Vector2)transform.position;
        Vector2 direction = _hasSample ? _sampleDirection : (Vector2)transform.right;
        float distance = _hasSample ? _sampleDistance : maxSeeAhead;

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(origin, KeepOutRadius());

        if (direction != Vector2.zero && distance > 0f)
        {
            Vector2 ahead = origin + direction.normalized * distance;
            Gizmos.DrawLine(origin, ahead);
            Gizmos.DrawWireSphere(ahead, KeepOutRadius());
        }

        if (_sampleVelocity == Vector2.zero)
            return;

        Gizmos.color = new Color(0.9f, 0.35f, 1f, 0.95f);
        Gizmos.DrawLine(origin, origin + _sampleVelocity);
    }
}
