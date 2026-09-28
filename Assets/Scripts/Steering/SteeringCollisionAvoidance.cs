using UnityEngine;

/// <summary>
/// Casts one feeler toward the goal. If it hits a wall, steer toward the goal and the open side.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    [SerializeField] LayerMask obstacleLayers = 1 << 3;
    [SerializeField] float maxSeeAhead = 3f;
    [SerializeField] float obstaclePadding = 0.4f;

    CircleCollider2D _body;
    int _side;

    public void Clear() => _side = 0;

    public Vector2 ResolveDesired(Vector2 position, Vector2 desiredVelocity)
    {
        if (desiredVelocity.sqrMagnitude < SteeringMath.Epsilon)
        {
            Remember(Vector2.zero, 0f, desiredVelocity);
            return desiredVelocity;
        }

        Vector2 ahead = desiredVelocity.normalized;
        float speed = desiredVelocity.magnitude;

        if (IsClear(position, ahead))
        {
            _side = 0;
            Remember(ahead, maxSeeAhead, desiredVelocity);
            return desiredVelocity;
        }

        Vector2 left = Side(ahead, 1);
        Vector2 right = Side(ahead, -1);
        bool leftClear = IsClear(position, left);
        bool rightClear = IsClear(position, right);

        if (leftClear && !rightClear)
            _side = 1;
        else if (rightClear && !leftClear)
            _side = -1;
        else if (_side == 0)
            _side = (GetInstanceID() & 1) == 0 ? 1 : -1;

        Vector2 resolved = (ahead + Side(ahead, _side)).normalized * speed;
        Remember(resolved.normalized, maxSeeAhead, resolved);
        return resolved;
    }

    static Vector2 Side(Vector2 ahead, int sign) => new Vector2(-ahead.y, ahead.x) * sign;

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

    bool IsClear(Vector2 position, Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.CircleCast(position, KeepOutRadius(), direction, maxSeeAhead, obstacleLayers);
        return hit.collider == null || hit.collider == _body;
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
