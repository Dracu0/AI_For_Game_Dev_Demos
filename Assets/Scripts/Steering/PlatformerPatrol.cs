using UnityEngine;

/// <summary>
/// Walks back and forth, turning around at ledges and walls. When the target comes within chaseRange
/// the PlatformerSeek on the same object takes over; when it leaves, patrolling resumes.
/// </summary>
[RequireComponent(typeof(PlatformerSeek), typeof(Rigidbody2D))]
public class PlatformerPatrol : MonoBehaviour
{
    [SerializeField] float patrolSpeed = 2f;
    [SerializeField] float chaseRange = 5f;

    PlatformerSeek _seek;
    Rigidbody2D _rb;
    float _dir = 1f; // -1 = left, +1 = right

    void Awake()
    {
        _seek = GetComponent<PlatformerSeek>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // Chase: the seeker steers while the target is in range, so we stay out of its way.
        bool targetInRange = Vector2.Distance(transform.position, _seek.Target.position) <= chaseRange;
        _seek.enabled = targetInRange;

        if (targetInRange)
            return;

        // Patrol: turn around at a ledge or a wall, then walk.
        if (_seek.LedgeAhead(_dir) || _seek.WallAhead(_dir))
            _dir = -_dir;

        Vector2 velocity = _rb.linearVelocity;
        velocity.x = _dir * patrolSpeed;
        _rb.linearVelocity = velocity;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
