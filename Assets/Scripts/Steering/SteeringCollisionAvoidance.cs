using UnityEngine;

/// <summary>
/// Goes straight at the goal. When a wall blocks the way it commits to a side
/// (the side with the nearest opening), slides along the wall, and only lets go
/// once the way to the goal has been clear for a short moment.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] LayerMask obstacleLayers = 1 << 3;
    [SerializeField] float maxSeeAhead = 3f;
    [SerializeField] float obstaclePadding = 0.4f;

    [Header("Side selection")]
    [Tooltip("Spacing of the sideways probes used to find the nearest opening.")]
    [SerializeField] float probeStep = 0.5f;
    [Tooltip("How many probes per side (reach = step * count).")]
    [SerializeField, Min(1)] int probeCount = 16;
    [Tooltip("Seconds the way must stay clear before the committed side is forgotten.")]
    [SerializeField] float commitHoldTime = 0.35f;
    [Tooltip("Minimum seconds between dead-end side flips.")]
    [SerializeField] float flipCooldown = 0.5f;

    CircleCollider2D _body;
    Vector2 _slide;                 // committed slide direction (unit). Zero = not committed.
    float _clearTime;               // time since the way ahead was last blocked
    float _lastFlipTime = -999f;
    readonly Collider2D[] _overlaps = new Collider2D[8];

    public void Clear()
    {
        _slide = Vector2.zero;
        _clearTime = 0f;
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
            Remember(Vector2.zero, 0f, desiredVelocity);
            return desiredVelocity;
        }

        Vector2 ahead = desiredVelocity.normalized;
        float speed = desiredVelocity.magnitude;
        float radius = KeepOutRadius();
        float seeAhead = Mathf.Min(maxSeeAhead, Mathf.Max(maxDistance, 0f));

        RaycastHit2D hit = Physics2D.CircleCast(position, radius, ahead, seeAhead, obstacleLayers);

        if (!IsObstacle(hit))
        {
            // Keep the committed side for a moment so we don't flip-flop around corners.
            _clearTime += Time.fixedDeltaTime;
            if (_clearTime > commitHoldTime)
                _slide = Vector2.zero;

            Remember(ahead, seeAhead, desiredVelocity);
            return desiredVelocity;
        }

        _clearTime = 0f;

        Vector2 normal = WallNormal(hit, position, ahead);
        Vector2 tangent = new Vector2(-normal.y, normal.x);

        if (_slide.sqrMagnitude < SteeringMath.Epsilon)
        {
            // First contact: pick the side with the nearest opening and stick with it.
            _slide = ChooseSlide(position, ahead, tangent, radius);
        }
        else
        {
            // Already committed: follow the wall's shape, never change direction because
            // the goal happens to be slightly to the other side.
            if (Vector2.Dot(tangent, _slide) < 0f)
                tangent = -tangent;
            _slide = tangent;

            // Dead end (inner corner)? Turn around, but not every frame.
            if (Time.time - _lastFlipTime > flipCooldown && BlockedAlong(position, _slide, radius))
            {
                _slide = -_slide;
                _lastFlipTime = Time.time;
            }
        }

        float closeness = 1f - Mathf.Clamp01(hit.distance / maxSeeAhead);
        float weight = closeness * closeness;

        Vector2 blended = Vector2.Lerp(ahead, _slide, weight);
        if (blended.sqrMagnitude < 0.01f)
            blended = _slide;

        Vector2 resolved = blended.normalized * speed;
        Remember(ahead, seeAhead, resolved);
        return resolved;
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

    /// <summary>
    /// When the cast starts touching/overlapping the wall (exactly what happens once
    /// PreventMovingIntoWalls has stopped us), Unity reports normal = -direction and
    /// distance 0. That is what made the enemy freeze head-on against walls, so in that
    /// case we rebuild the normal from the closest point on the collider instead.
    /// </summary>
    Vector2 WallNormal(RaycastHit2D hit, Vector2 position, Vector2 ahead)
    {
        if (hit.distance > 0.001f && hit.normal.sqrMagnitude > SteeringMath.Epsilon)
            return hit.normal.normalized;

        Vector2 away = position - hit.collider.ClosestPoint(position);
        if (away.sqrMagnitude > SteeringMath.Epsilon)
            return away.normalized;

        return -ahead;
    }

    /// <summary>Picks the tangent direction that leads to the nearest opening.</summary>
    Vector2 ChooseSlide(Vector2 position, Vector2 ahead, Vector2 tangent, float radius)
    {
        float plus = DistanceToOpening(position, ahead, tangent, radius);
        float minus = DistanceToOpening(position, ahead, -tangent, radius);

        bool pickPlus;
        if ((float.IsInfinity(plus) && float.IsInfinity(minus)) || Mathf.Abs(plus - minus) < probeStep * 0.5f)
        {
            // No info / tie: prefer the side the goal leans toward, otherwise a stable per-enemy choice.
            float towardGoal = Vector2.Dot(tangent, ahead);
            pickPlus = Mathf.Abs(towardGoal) > 0.05f ? towardGoal > 0f : (GetInstanceID() & 1) == 0;
        }
        else
        {
            pickPlus = plus < minus;
        }

        return pickPlus ? tangent : -tangent;
    }

    /// <summary>
    /// Walks sideways in small steps and returns how far we have to go before a
    /// keep-out-sized cast toward the goal is no longer blocked. Infinity = none found.
    /// </summary>
    float DistanceToOpening(Vector2 position, Vector2 ahead, Vector2 side, float radius)
    {
        float reach = probeStep * probeCount;

        // Can't probe through other walls to the side.
        RaycastHit2D lateral = Physics2D.CircleCast(position, radius * 0.5f, side, reach, obstacleLayers);
        if (IsObstacle(lateral))
            reach = lateral.distance;

        for (float offset = probeStep; offset <= reach; offset += probeStep)
        {
            RaycastHit2D h = Physics2D.CircleCast(position + side * offset, radius, ahead, maxSeeAhead, obstacleLayers);
            if (!IsObstacle(h))
                return offset;
        }

        return float.PositiveInfinity;
    }

    bool BlockedAlong(Vector2 position, Vector2 direction, float radius)
    {
        // Smaller circle so the wall we are sliding along doesn't count as blocking.
        RaycastHit2D h = Physics2D.CircleCast(position, radius * 0.5f, direction, radius + 0.25f, obstacleLayers);
        return IsObstacle(h);
    }

    void Awake() => _body = GetComponent<CircleCollider2D>();

    void OnDisable() => Clear();

    float KeepOutRadius()
    {
        if (_body == null)
            _body = GetComponent<CircleCollider2D>();
        if (_body == null)
            return Mathf.Max(0f, obstaclePadding);

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        return _body.radius * scale + Mathf.Max(0f, obstaclePadding);
    }

    // -------------------------------------------------------------------------
    // Gizmos — yellow = feeler, magenta = direction actually used
    // -------------------------------------------------------------------------

    [SerializeField] bool showGizmos = true;

    float _sampleTime = -1f;
    Vector2 _sampleDirection;
    float _sampleDistance;
    Vector2 _sampleVelocity;

    void Remember(Vector2 direction, float distance, Vector2 velocity)
    {
        _sampleTime = Time.time;
        _sampleDirection = direction;
        _sampleDistance = distance;
        _sampleVelocity = velocity;
    }

    bool SampleIsCurrent() =>
        Application.isPlaying && Time.time - _sampleTime <= Mathf.Max(Time.fixedDeltaTime * 2f, 0.05f);

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        bool current = SampleIsCurrent();
        Vector2 origin = transform.position;
        float radius = KeepOutRadius();

        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(origin, radius);

        if (!current && Application.isPlaying)
            return;

        Vector2 direction = current ? _sampleDirection : (Vector2)transform.right;
        float distance = current ? _sampleDistance : maxSeeAhead;
        if (direction != Vector2.zero && distance > 0f)
        {
            Vector2 ahead = origin + direction.normalized * distance;
            Gizmos.DrawLine(origin, ahead);
            Gizmos.DrawWireSphere(ahead, radius);
        }

        if (!current || _sampleVelocity == Vector2.zero)
            return;

        Gizmos.color = new Color(0.9f, 0.35f, 1f, 0.95f);
        Gizmos.DrawLine(origin, origin + _sampleVelocity);
    }
}
