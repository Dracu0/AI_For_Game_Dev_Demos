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

    [Header("Detection")]
    [SerializeField] Transform rangeCircle;
    [InspectorName("Detection Radius")]
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
    bool _bypassSteeringRange;

    public Transform Player => player;
    public bool IsSteeringActive => _steeringActive;
    public float Range => range;

    /// <param name="bypassRangeLimit">
    /// When true, chase logic can pursue anywhere inside detection (not capped by <see cref="range"/>).
    /// </param>
    public void SetSteeringActive(bool active, bool bypassRangeLimit = false)
    {
        if (_steeringActive == active && _bypassSteeringRange == bypassRangeLimit)
            return;

        _steeringActive = active;
        _bypassSteeringRange = active && bypassRangeLimit;
        EnsureRigidbody();
        if (_rb == null)
            return;

        if (active)
            _rb.position = transform.position;
        else
        {
            _bypassSteeringRange = false;
            SteeringMath.Stop(_rb);
        }
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

    void OnValidate() => ScheduleRangeVisualRefresh();

    void ScheduleRangeVisualRefresh()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RefreshRangeVisualsIfAlive;
        UnityEditor.EditorApplication.delayCall += RefreshRangeVisualsIfAlive;
#else
        RefreshRangeVisuals();
#endif
    }

    void RefreshRangeVisualsIfAlive()
    {
        if (this == null)
            return;

        RefreshRangeVisuals();
    }

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

        if (!_bypassSteeringRange && SteeringMath.IsOutOfRange(from, to, range))
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

    public void RefreshRangeVisuals()
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

        float spriteWidth = sprite.sprite.bounds.size.x;
        float parentScale = circle.parent != null ? Mathf.Abs(circle.parent.lossyScale.x) : 1f;
        if (spriteWidth <= 0f || parentScale <= 0f)
            return;

        Vector3 scale = Vector3.one * (radius * 2f / (spriteWidth * parentScale));
        if ((circle.localScale - scale).sqrMagnitude < 0.000001f)
            return;

        circle.localScale = scale;
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

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            RefreshRangeVisuals();

        if (!showRangeGizmo)
            return;

        Gizmos.color = Visible(rangeGizmoColor);
        DrawCircleGizmo(rangeCircle, transform.position, range);

        if (slowRadiusCircle == null || slowRadiusCircle == rangeCircle)
            return;

        Gizmos.color = Visible(slowRadiusGizmoColor);
        DrawCircleGizmo(slowRadiusCircle, transform.position, slowRadius);
    }

    static Color Visible(Color color) => new Color(color.r, color.g, color.b, Mathf.Max(color.a, 0.9f));

    static void DrawCircleGizmo(Transform circle, Vector3 fallbackCenter, float radius)
    {
        Vector3 center = circle != null ? circle.position : fallbackCenter;
        Gizmos.DrawWireSphere(center, Mathf.Max(0f, radius));
    }
}
