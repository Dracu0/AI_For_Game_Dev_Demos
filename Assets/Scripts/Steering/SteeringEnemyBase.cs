using UnityEngine;

/// <summary>
/// Range-based steering enemies: subclasses supply desired velocity; this class handles physics.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class SteeringEnemyBase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] protected Transform player;

    [Header("Movement")]
    [SerializeField] protected float maxSpeed = 3f;
    [SerializeField] protected float maxForce = 6f;

    Rigidbody2D _rb;
    Rigidbody2D _playerRb;
    SteeringCollisionAvoidance _avoidance;

    protected Rigidbody2D Body => _rb;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _avoidance = GetComponent<SteeringCollisionAvoidance>();
        SteeringMath.SetupEnemy(_rb);
        CachePlayerRigidbody();
        RefreshRangeVisuals();
    }

    protected virtual void OnValidate() => RefreshRangeVisuals();

    protected abstract void RefreshRangeVisuals();
    protected abstract bool TryGetDesiredVelocity(out Vector2 desired);

    protected bool IsPlayerBeyondRange(float range) =>
        SteeringMath.IsOutOfRange(Body.position, player.position, range);

    protected Vector2 PredictPlayerPosition(float targetMaxSpeed)
    {
        Vector2 playerVelocity = _playerRb != null ? _playerRb.linearVelocity : Vector2.zero;
        return SteeringMath.PredictPosition(Body.position, player.position, playerVelocity, targetMaxSpeed);
    }

    void CachePlayerRigidbody()
    {
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
    }

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
}
