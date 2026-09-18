using UnityEngine;

/// <summary>
/// Shared setup and physics steering for range-based enemy behaviours.
/// Subclasses only define when to move and what desired velocity to use.
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
    SteeringCollisionAvoidance _avoidance;

    protected Rigidbody2D Body => _rb;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _avoidance = GetComponent<SteeringCollisionAvoidance>();
        SteeringMath.SetupEnemy(_rb);
        RefreshRangeVisuals();
    }

    protected virtual void OnValidate() => RefreshRangeVisuals();

    /// <summary>Resize optional range gizmos in the editor and at runtime.</summary>
    protected abstract void RefreshRangeVisuals();

    /// <summary>Return false to stop; true to steer toward desired.</summary>
    protected abstract bool TryGetDesiredVelocity(out Vector2 desired);

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
