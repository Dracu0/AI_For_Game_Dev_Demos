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
/// One steering enemy. Every mode does the same thing:
/// pick a desired velocity, then steer toward it.
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
    bool _active = true;
    bool _moveToPoint;
    Vector2 _point;

    public Transform Player => player;
    public float Range => range;

    public void SetSteeringActive(bool active)
    {
        if (_active == active)
            return;

        _active = active;
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            return;

        if (active)
            _rb.position = transform.position;
        else
            SteeringMath.Stop(_rb);
    }

    public void SetMoveTarget(Vector2 point)
    {
        _moveToPoint = true;
        _point = point;
    }

    public void ClearMoveTarget() => _moveToPoint = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _avoidance = GetComponent<SteeringCollisionAvoidance>();
        SteeringMath.SetupBody(_rb);
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
        ResizeCircles();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= ResizeCircles;
        UnityEditor.EditorApplication.delayCall += ResizeCircles;
#endif
    }

    void FixedUpdate()
    {
        if (!_active)
            return;

        if (!_moveToPoint && (player == null || Vector2.Distance(_rb.position, player.position) > range))
        {
            SteeringMath.Stop(_rb);
            return;
        }

        _rb.linearVelocity = SteeringMath.Steer(_rb, DesiredVelocity(), maxForce, maxSpeed, _avoidance);
    }

    Vector2 DesiredVelocity()
    {
        if (_moveToPoint)
            return SteeringMath.SeekVelocity(_rb.position, _point, maxSpeed);

        Vector2 from = _rb.position;
        Vector2 to = player.position;

        switch (mode)
        {
            case SteeringMode.Seek:
                return SteeringMath.SeekVelocity(from, to, maxSpeed);
            case SteeringMode.Flee:
                return SteeringMath.FleeVelocity(from, to, maxSpeed);
            case SteeringMode.Arrival:
                return SteeringMath.ArrivalVelocity(from, to, maxSpeed, slowRadius);
            case SteeringMode.Pursue:
                return SteeringMath.SeekVelocity(from, PredictedPlayerPosition(), maxSpeed);
            case SteeringMode.Evade:
                return SteeringMath.FleeVelocity(from, PredictedPlayerPosition(), maxSpeed);
            default:
                return Vector2.zero;
        }
    }

    Vector2 PredictedPlayerPosition()
    {
        Vector2 playerVelocity = _playerRb != null ? _playerRb.linearVelocity : Vector2.zero;
        return SteeringMath.PredictPosition(_rb.position, player.position, playerVelocity, targetMaxSpeed);
    }

    void ResizeCircles()
    {
        if (this == null)
            return;

        ResizeCircle(rangeCircle, range);
        if (slowRadiusCircle != null && slowRadiusCircle != rangeCircle)
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
        if (circle.localScale != scale)
            circle.localScale = scale;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            ResizeCircles();

        Gizmos.color = new Color(1f, 1f, 1f, 0.9f);
        Gizmos.DrawWireSphere(CircleCenter(rangeCircle), range);

        if (slowRadiusCircle == null || slowRadiusCircle == rangeCircle)
            return;

        Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireSphere(CircleCenter(slowRadiusCircle), slowRadius);
    }

    Vector3 CircleCenter(Transform circle) => circle != null ? circle.position : transform.position;
}
