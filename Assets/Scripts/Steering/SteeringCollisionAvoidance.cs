using UnityEngine;

/// <summary>
/// Goes straight at the goal. Near a wall, it slides toward the nearer end instead of arcing early.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    [SerializeField] LayerMask obstacleLayers = 1 << 3;
    [SerializeField] float maxSeeAhead = 3f;
    [SerializeField] float obstaclePadding = 0.4f;

    CircleCollider2D _body;
    Vector2 _slide;

    public void Clear() => _slide = Vector2.zero;

    public Vector2 ResolveDesired(Vector2 position, Vector2 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude < SteeringMath.Epsilon)
        {
            Remember(Vector2.zero, 0f, desiredVelocity);
            return desiredVelocity;
        }

        Vector2 ahead = desiredVelocity.normalized;
        float speed = desiredVelocity.magnitude;
        RaycastHit2D hit = Physics2D.CircleCast(position, KeepOutRadius(), ahead, maxSeeAhead, obstacleLayers);

        if (hit.collider == null || hit.collider == _body || hit.normal.sqrMagnitude < SteeringMath.Epsilon)
        {
            _slide = Vector2.zero;
            Remember(ahead, maxSeeAhead, desiredVelocity);
            return desiredVelocity;
        }

        float closeness = 1f - Mathf.Clamp01(hit.distance / maxSeeAhead);
        Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
        float towardGoal = Vector2.Dot(tangent, ahead);

        if (Mathf.Abs(towardGoal) > 0.2f)
            _slide = towardGoal >= 0f ? tangent : -tangent;
        else if (_slide == Vector2.zero)
            _slide = tangent * ((GetInstanceID() & 1) == 0 ? 1 : -1);
        else if (Vector2.Dot(tangent, _slide) < 0f)
            _slide = -tangent;

        Vector2 resolved = Vector2.Lerp(ahead, _slide.normalized, closeness * closeness).normalized * speed;
        Remember(ahead, maxSeeAhead, resolved);
        return resolved;
    }

    public Vector2 PreventMovingIntoWalls(Vector2 position, Vector2 velocity)
    {
        if (velocity.sqrMagnitude < SteeringMath.Epsilon)
            return velocity;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, KeepOutRadius(), obstacleLayers);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider2D wall = overlaps[i];
            if (wall == null || wall == _body)
                continue;

            Vector2 away = position - wall.ClosestPoint(position);
            if (away.sqrMagnitude < SteeringMath.Epsilon)
                continue;

            float pushingIn = Vector2.Dot(velocity, -away.normalized);
            if (pushingIn > 0f)
                velocity += away.normalized * pushingIn;
        }

        return velocity;
    }

    void Awake() => _body = GetComponent<CircleCollider2D>();

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
