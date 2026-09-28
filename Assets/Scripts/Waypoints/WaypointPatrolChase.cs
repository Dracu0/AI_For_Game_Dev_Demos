using UnityEngine;

/// <summary>
/// Patrol, chase, walk to the last seen point, wait, then patrol from the nearest waypoint.
/// </summary>
[RequireComponent(typeof(WaypointPatrol), typeof(SteeringEnemy))]
[DefaultExecutionOrder(-10)]
public class WaypointPatrolChase : MonoBehaviour
{
    enum Phase { Patrol, Chase, LastSeen, Wait }

    [SerializeField] float returnToPatrolDelay = 2f;
    [SerializeField] float arriveDistance = 0.15f;

    WaypointPatrol _patrol;
    SteeringEnemy _steering;
    Rigidbody2D _rb;

    Phase _phase;
    Vector2 _lastSeen;
    float _wait;

    void Awake()
    {
        _patrol = GetComponent<WaypointPatrol>();
        _steering = GetComponent<SteeringEnemy>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start() => Apply(Phase.Patrol);

    void FixedUpdate()
    {
        Transform player = _steering.Player;
        if (player == null)
            return;

        if (Vector2.Distance(_rb.position, player.position) <= _steering.Range)
        {
            _lastSeen = player.position;
            SetPhase(Phase.Chase);
            return;
        }

        if (_phase == Phase.Chase)
            SetPhase(Phase.LastSeen);
        else if (_phase == Phase.LastSeen && Vector2.Distance(_rb.position, _lastSeen) <= arriveDistance)
            SetPhase(Phase.Wait);
        else if (_phase == Phase.Wait)
        {
            _wait += Time.fixedDeltaTime;
            if (_wait >= returnToPatrolDelay)
                SetPhase(Phase.Patrol);
        }
    }

    void SetPhase(Phase phase)
    {
        if (_phase == phase)
            return;

        Apply(phase);
    }

    void Apply(Phase phase)
    {
        _phase = phase;

        bool steering = phase == Phase.Chase || phase == Phase.LastSeen;
        _rb.bodyType = steering ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;

        _steering.SetSteeringActive(steering);
        if (phase == Phase.LastSeen)
            _steering.SetMoveTarget(_lastSeen);
        else
            _steering.ClearMoveTarget();

        _patrol.SetActive(phase == Phase.Patrol);
        if (phase == Phase.Wait)
            _wait = 0f;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (_phase != Phase.LastSeen && _phase != Phase.Wait)
            return;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(_lastSeen, arriveDistance);
        Gizmos.DrawLine(_rb != null ? (Vector3)_rb.position : transform.position, _lastSeen);
    }
}
