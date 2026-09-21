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

    public void Bind(Transform target, SteeringMode steeringMode)
    {
        player = target;
        mode = steeringMode;
        CachePlayerRigidbody();
        RefreshRangeVisuals();
    }

    public void SetRangeVisuals(Transform range, Transform slow = null)
    {
        rangeCircle = range;
        slowRadiusCircle = slow;
        RefreshRangeVisuals();
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _avoidance = GetComponent<SteeringCollisionAvoidance>();
        SteeringMath.SetupEnemy(_rb);
        CachePlayerRigidbody();
        RefreshRangeVisuals();
    }

    void OnValidate() => RefreshRangeVisuals();

    void FixedUpdate()
    {
        if (player == null)
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
                desired = SteeringMath.ArrivalVelocity(
                    from, to, Vector2.Distance(from, to), maxSpeed, slowRadius);
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
        SteeringMath.ResizeCircle(rangeCircle, range);

        if (slowRadiusCircle == null || slowRadiusCircle == rangeCircle)
            return;

        SteeringMath.ResizeCircle(slowRadiusCircle, slowRadius);
    }

    void CachePlayerRigidbody()
    {
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
    }
}
