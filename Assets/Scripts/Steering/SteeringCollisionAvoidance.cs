using UnityEngine;

/// <summary>
/// Keeps a circular body off walls while it heads for its goal.
/// It steers along one heading that turns a little every physics step: away from a wall while the
/// heading is blocked, and back toward the goal whenever the turned heading is still clear.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] LayerMask obstacleLayers = 1 << 3;
    [SerializeField] float maxSeeAhead = 3f;
    [SerializeField] float obstaclePadding = 0.4f;

    [Header("Turning")]
    [Tooltip("How far the heading can turn each physics step, in degrees.")]
    [SerializeField, Range(1f, 10f)] float turnStep = 3f;
    [SerializeField] bool showGizmos = true;

    CircleCollider2D _body;
    readonly Collider2D[] _overlaps = new Collider2D[8];

    Vector2 _heading; // unit direction we are steering along. Zero = not set yet.
    bool _turning;    // true while the heading is blocked and we are turning away from a wall
    float _turnSign;  // +1 = counter-clockwise, -1 = clockwise. Only used while _turning.

    public void Clear()
    {
        _heading = Vector2.zero;
        _turning = false;
    }

    /// <summary>
    /// maxDistance = how far away the goal is. Walls behind the goal are ignored.
    /// Pass infinity for flee-style behaviours.
    /// </summary>
    public Vector2 ResolveDesired(Vector2 position, Vector2 desiredVelocity, float maxDistance = float.PositiveInfinity)
    {
        if (desiredVelocity.sqrMagnitude < SteeringMath.Epsilon)
        {
            Clear();
            return desiredVelocity;
        }

        Vector2 goal = desiredVelocity.normalized;
        float speed = desiredVelocity.magnitude;
        float reach = Mathf.Min(maxSeeAhead, Mathf.Max(maxDistance, 0f));
        if (_heading == Vector2.zero)
            _heading = goal;

        RaycastHit2D hit = Cast(position, _heading, reach);
        if (IsObstacle(hit))
        {
            if (!_turning)
            {
                // New obstacle: turn toward the nearer opening, and keep turning that way until clear.
                _turning = true;
                _turnSign = NearestOpeningSign(position, _heading);
            }

            _heading = Rotate(_heading, _turnSign * turnStep);
        }
        else
        {
            _turning = false;

            // Clear: swing back toward the goal, but only into headings that are also clear.
            float toGoal = Mathf.Atan2(Cross(_heading, goal), Vector2.Dot(_heading, goal)) * Mathf.Rad2Deg;
            if (Mathf.Abs(toGoal) > 0.01f)
            {
                Vector2 next = Rotate(_heading, Mathf.Clamp(toGoal, -turnStep, turnStep));
                if (!IsObstacle(Cast(position, next, reach)))
                    _heading = next;
            }
        }

        return _heading * speed;
    }

    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < SteeringMath.Epsilon)
            return velocity;

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.SetLayerMask(obstacleLayers);

        int count = Physics2D.OverlapCircle(position, KeepOutRadius(), filter, _overlaps);
        for (int i = 0; i < count; i++)
        {
            Collider2D wall = _overlaps[i];
            if (wall == null || wall == _body)
                continue;

            Vector2 away = position - wall.ClosestPoint(position);
            if (away.sqrMagnitude < SteeringMath.Epsilon)
                away = position - (Vector2)wall.bounds.center; // we are inside it: push out from its centre
            if (away.sqrMagnitude < SteeringMath.Epsilon)
                continue;

            Vector2 n = away.normalized;
            float pushingIn = -Vector2.Dot(velocity, n);
            if (pushingIn > 0f)
                velocity += n * pushingIn;
        }

        return velocity;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    bool IsObstacle(RaycastHit2D hit) => hit.collider != null && hit.collider != _body;

    RaycastHit2D Cast(Vector2 position, Vector2 direction, float distance) =>
        Physics2D.CircleCast(position, CastRadius(), direction, distance, obstacleLayers);

    /// <summary>
    /// Sweeps outward from the heading on both sides and returns the side whose clear direction is nearer.
    /// Ties go to a side that is fixed per agent, so a head-on hit doesn't turn every agent the same way.
    /// </summary>
    float NearestOpeningSign(Vector2 position, Vector2 heading)
    {
        float preferred = (GetInstanceID() & 1) == 0 ? 1f : -1f;
        for (float angle = 15f; angle <= 180f; angle += 15f)
        {
            if (!IsObstacle(Cast(position, Rotate(heading, preferred * angle), maxSeeAhead)))
                return preferred;
            if (!IsObstacle(Cast(position, Rotate(heading, -preferred * angle), maxSeeAhead)))
                return -preferred;
        }

        return preferred;
    }

    static Vector2 Rotate(Vector2 direction, float degrees) => Quaternion.Euler(0f, 0f, degrees) * direction;

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    void Awake() => _body = GetComponent<CircleCollider2D>();

    void OnDisable() => Clear();

    float BodyRadius()
    {
        if (_body == null)
            _body = GetComponent<CircleCollider2D>();
        if (_body == null)
            return 0f;

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        return _body.radius * scale;
    }

    float KeepOutRadius() => BodyRadius() + Mathf.Max(0f, obstaclePadding);

    /// <summary>
    /// Casts use a circle a little smaller than the keep-out ring, so a body sliding along a wall (which sits
    /// right on that ring) isn't counted as blocked.
    /// </summary>
    float CastRadius() => BodyRadius() + Mathf.Max(0f, obstaclePadding) * 0.75f;

    // -------------------------------------------------------------------------
    // Gizmos: yellow = keep-out ring, magenta = heading being steered along
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Vector2 origin = transform.position;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(origin, KeepOutRadius());

        if (_heading == Vector2.zero)
            return;

        Gizmos.color = new Color(0.9f, 0.35f, 1f, 0.95f);
        Gizmos.DrawLine(origin, origin + _heading * maxSeeAhead);
    }
}
