using UnityEngine;

public enum SteeringMode
{
    Seek,
    Flee,
    Arrival,
    Pursue,
    Evade
}

/// <summary>
/// One steering enemy. Pick the behaviour in the Inspector.
/// All modes share the same loop: compute a desired velocity, then steer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SteeringEnemy : MonoBehaviour
{
    [Header("Behaviour")]
    [SerializeField] SteeringMode mode = SteeringMode.Seek;

    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    [Header("Range")]
    [SerializeField] Transform rangeCircle;
    [SerializeField] float range = 5f;

    [Header("Arrival only")]
    [SerializeField] Transform slowRadiusCircle;
    [SerializeField] float slowRadius = 3f;

    [Header("Pursue / Evade only")]
    [SerializeField] float targetMaxSpeed = 5f;

    Rigidbody2D _rb;
    Rigidbody2D _playerRb;
    SteeringCollisionAvoidance _avoidance;
    bool _steeringActive = true;

    public Transform Player => player;
    public bool IsSteeringActive => _steeringActive;

    public void SetSteeringActive(bool active)
    {
        if (_steeringActive == active)
            return;

        _steeringActive = active;
        EnsureRigidbody();
        if (_rb == null)
            return;

        if (active)
            _rb.position = transform.position;
        else
            SteeringMath.Stop(_rb);
    }

    public void Bind(Transform target, SteeringMode steeringMode)
    {
        player = target;
        mode = steeringMode;
        CachePlayerRigidbody();
        RefreshRangeVisuals();
    }

    void EnsureRigidbody()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        EnsureRigidbody();
        _avoidance = GetComponent<SteeringCollisionAvoidance>();
        SteeringMath.SetupBody(_rb);
        CachePlayerRigidbody();
        RefreshRangeVisuals();
    }

    void OnValidate() => RefreshRangeVisuals();

    void FixedUpdate()
    {
        if (!_steeringActive || player == null)
            return;

        if (!TryGetDesiredVelocity(out Vector2 desired))
        {
            SteeringMath.Stop(_rb);
            return;
        }

        _rb.linearVelocity = SteeringMath.Steer(_rb, desired, maxForce, maxSpeed, _avoidance);
    }

    bool TryGetDesiredVelocity(out Vector2 desired)
    {
        Vector2 from = _rb.position;
        Vector2 to = player.position;

        if (SteeringMath.IsOutOfRange(from, to, range))
        {
            desired = default;
            return false;
        }

        switch (mode)
        {
            case SteeringMode.Seek:
                desired = SteeringMath.SeekVelocity(from, to, maxSpeed);
                return true;

            case SteeringMode.Flee:
                desired = SteeringMath.FleeVelocity(from, to, maxSpeed);
                return true;

            case SteeringMode.Arrival:
                desired = SteeringMath.ArrivalVelocity(from, to, maxSpeed, slowRadius);
                return true;

            case SteeringMode.Pursue:
                desired = SteeringMath.SeekVelocity(from, PredictedPlayerPosition(), maxSpeed);
                return true;

            case SteeringMode.Evade:
                desired = SteeringMath.FleeVelocity(from, PredictedPlayerPosition(), maxSpeed);
                return true;

            default:
                desired = default;
                return false;
        }
    }

    Vector2 PredictedPlayerPosition()
    {
        Vector2 playerVelocity = _playerRb != null ? _playerRb.linearVelocity : Vector2.zero;
        return SteeringMath.PredictPosition(_rb.position, player.position, playerVelocity, targetMaxSpeed);
    }

    void RefreshRangeVisuals()
    {
        ResizeCircle(rangeCircle, range);

        if (slowRadiusCircle == null || slowRadiusCircle == rangeCircle)
            return;

        ResizeCircle(slowRadiusCircle, slowRadius);
    }

    static void ResizeCircle(Transform circle, float radius)
    {
        if (circle == null)
            return;

        SpriteRenderer sprite = circle.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return;

        float diameter = sprite.sprite.bounds.size.x;
        if (diameter <= 0f)
            return;

        circle.localScale = Vector3.one * (radius * 2f / diameter);
    }

    void CachePlayerRigidbody()
    {
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    [SerializeField] bool showRangeGizmo = true;
    [SerializeField] Color rangeGizmoColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] Color slowRadiusGizmoColor = new Color(0.5f, 1f, 0.5f, 0.2f);

    void OnDrawGizmosSelected()
    {
        if (!showRangeGizmo)
            return;

        Vector3 center = transform.position;

        Gizmos.color = rangeGizmoColor;
        DrawGizmoCircle(center, range);

        if (mode != SteeringMode.Arrival || slowRadiusCircle == rangeCircle)
            return;

        Gizmos.color = slowRadiusGizmoColor;
        DrawGizmoCircle(center, slowRadius);
    }

    static void DrawGizmoCircle(Vector3 center, float radius)
    {
        if (radius <= 0f)
            return;

        const int segments = 24;
        Vector2 previous = (Vector2)center + Vector2.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector2 next = (Vector2)center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
