using UnityEngine;

/// <summary>
/// Patrols until the player enters the detection radius, then steers.
/// When the player leaves, walks to the last seen point, waits, then patrols again.
/// </summary>
[RequireComponent(typeof(WaypointPatrol), typeof(SteeringEnemy))]
[DefaultExecutionOrder(-10)]
public class WaypointPatrolChase : MonoBehaviour
{
    enum Phase { Patrol, Chase, LastSeen, Wait }

    [SerializeField] float returnToPatrolDelay = 2f;
    [SerializeField] float investigateSpeed = 3f;
    [SerializeField] float arriveDistance = 0.15f;

    WaypointPatrol _patrol;
    SteeringEnemy _steering;
    Rigidbody2D _rb;

    Phase _phase = Phase.Patrol;
    Vector2 _lastSeen;
    float _wait;

    void Awake()
    {
        _patrol = GetComponent<WaypointPatrol>();
        _steering = GetComponent<SteeringEnemy>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start() => BeginPatrol();

    void FixedUpdate()
    {
        Transform player = _steering.Player;
        if (player == null)
            return;

        Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;
        if (Vector2.Distance(pos, player.position) <= _steering.Range)
        {
            _lastSeen = player.position;
            BeginChase();
            return;
        }

        switch (_phase)
        {
            case Phase.Chase:
                BeginLastSeen();
                break;
            case Phase.LastSeen:
                MoveToLastSeen();
                break;
            case Phase.Wait:
                WaitThenPatrol();
                break;
        }
    }

    void BeginChase()
    {
        if (_phase == Phase.Chase)
            return;

        _phase = Phase.Chase;
        _patrol.SetActive(false);
        SetBody(steering: true);
        _steering.SetSteeringActive(true);
    }

    void BeginLastSeen()
    {
        _phase = Phase.LastSeen;
        _wait = 0f;
        _steering.SetSteeringActive(false);
        _patrol.SetActive(false);
        SetBody(steering: true);

        if (_rb != null)
            transform.position = _rb.position;
    }

    void MoveToLastSeen()
    {
        if (Vector2.Distance(_rb.position, _lastSeen) <= arriveDistance)
        {
            SetBody(steering: false);
            _phase = Phase.Wait;
            _wait = 0f;
            return;
        }

        _steering.SteerToward(_lastSeen, investigateSpeed);
    }

    void WaitThenPatrol()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        _wait += Time.fixedDeltaTime;
        if (_wait >= returnToPatrolDelay)
            BeginPatrol();
    }

    void BeginPatrol()
    {
        _phase = Phase.Patrol;
        SetBody(steering: false);
        _steering.SetSteeringActive(false);
        _patrol.SetActive(true);
    }

    void SetBody(bool steering)
    {
        if (_rb == null)
            return;

        _rb.bodyType = steering ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (_phase != Phase.LastSeen && _phase != Phase.Wait)
            return;

        Vector3 origin = Application.isPlaying && _rb != null ? (Vector3)_rb.position : transform.position;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(_lastSeen, arriveDistance);
        Gizmos.DrawLine(origin, _lastSeen);
    }
}
