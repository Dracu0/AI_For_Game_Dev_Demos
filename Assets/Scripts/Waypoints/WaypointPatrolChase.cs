using UnityEngine;

/// <summary>
/// Patrol until the player enters detection, then steering.
/// When the player leaves, walk to the last seen point, wait, then patrol again.
/// </summary>
[RequireComponent(typeof(WaypointPatrol), typeof(SteeringEnemy))]
[DefaultExecutionOrder(-10)]
public class WaypointPatrolChase : MonoBehaviour
{
    enum Phase { Patrol, Chase, GoToLastSeen, Wait }

    [SerializeField] float detectionRadius = 5f;
    [SerializeField] float returnToPatrolDelay = 2f;
    [SerializeField] float investigateSpeed = 3f;
    [SerializeField] float arriveDistance = 0.15f;

    public float DetectionRadius => detectionRadius;

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

    void Start()
    {
        UseSteering(false);
        _steering.SetSteeringActive(false);
        _patrol.SetActive(true);
    }

    void FixedUpdate()
    {
        Transform player = _steering.Player;
        if (player == null)
            return;

        Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;
        if (Vector2.Distance(pos, player.position) <= detectionRadius)
        {
            _lastSeen = player.position;
            StartChase();
            return;
        }

        if (_phase == Phase.Chase)
            StartGoToLastSeen();
        else if (_phase == Phase.GoToLastSeen)
            GoToLastSeen();
        else if (_phase == Phase.Wait)
            WaitThenPatrol();
    }

    void StartChase()
    {
        if (_phase == Phase.Chase)
            return;

        _phase = Phase.Chase;
        _patrol.SetActive(false);
        UseSteering(true);
        _steering.SetSteeringActive(true, bypassRangeLimit: true);
    }

    void StartGoToLastSeen()
    {
        _phase = Phase.GoToLastSeen;
        _wait = 0f;
        _steering.SetSteeringActive(false);
        _patrol.SetActive(false);
        UseSteering(false);

        if (_rb != null)
            transform.position = _rb.position;
    }

    void GoToLastSeen()
    {
        if (!AgentMove2D.StepTowards(transform, _rb, _lastSeen, investigateSpeed, arriveDistance))
            return;

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        _phase = Phase.Wait;
        _wait = 0f;
    }

    void WaitThenPatrol()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        _wait += Time.fixedDeltaTime;
        if (_wait < returnToPatrolDelay)
            return;

        _phase = Phase.Patrol;
        UseSteering(false);
        _patrol.SetActive(true);
    }

    void UseSteering(bool on)
    {
        if (_rb == null)
            return;

        _rb.bodyType = on ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    [SerializeField] bool showLastSeenGizmo = true;
    [SerializeField] Color lastSeenGizmoColor = new Color(1f, 0.6f, 0.2f, 0.85f);

    void OnDrawGizmos()
    {
        if (!showLastSeenGizmo || _phase != Phase.GoToLastSeen && _phase != Phase.Wait)
            return;

        Gizmos.color = lastSeenGizmoColor;
        Gizmos.DrawWireSphere(_lastSeen, arriveDistance);
        Gizmos.DrawLine(transform.position, _lastSeen);
    }
}
