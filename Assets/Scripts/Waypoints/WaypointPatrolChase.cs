using UnityEngine;

/// <summary>
/// Patrol until the player enters detection, then steering.
/// When the player leaves, walk to the last seen point, wait, then patrol again.
/// Detection radius is the only chase range. SteeringEnemy.range is not used while this runs.
/// </summary>
[RequireComponent(typeof(WaypointPatrol), typeof(SteeringEnemy))]
[DefaultExecutionOrder(-10)]
public class WaypointPatrolChase : MonoBehaviour
{
    enum Phase
    {
        Patrolling,
        Chasing,
        InvestigateMove,
        InvestigateWait
    }

    [SerializeField] float detectionRadius = 5f;
    [SerializeField] float returnToPatrolDelay = 2f;
    [SerializeField] float investigateSpeed = 3f;
    [SerializeField] float investigateArriveDistance = 0.15f;

    WaypointPatrol _patrol;
    SteeringEnemy _steering;
    Rigidbody2D _rb;

    Phase _phase = Phase.Patrolling;
    Vector2 _lastSeenPosition;
    float _waitAtLastSeen;
    float _stuckTime;
    float _closestDistance;

    void Awake()
    {
        _patrol = GetComponent<WaypointPatrol>();
        _steering = GetComponent<SteeringEnemy>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        SetDrivenBySteering(false);
        _steering.SetSteeringActive(false);
        _patrol.SetPatrolActive(true);
        _phase = Phase.Patrolling;
    }

    void FixedUpdate()
    {
        Transform player = _steering.Player;
        if (player == null)
            return;

        Vector2 enemyPos = _rb != null ? _rb.position : (Vector2)transform.position;
        bool playerDetected = Vector2.Distance(enemyPos, player.position) <= detectionRadius;

        if (playerDetected)
        {
            _lastSeenPosition = player.position;
            EnterChase();
            return;
        }

        switch (_phase)
        {
            case Phase.Chasing:
                EnterInvestigate();
                break;
            case Phase.InvestigateMove:
                UpdateInvestigateMove();
                break;
            case Phase.InvestigateWait:
                UpdateInvestigateWait();
                break;
        }
    }

    void EnterChase()
    {
        if (_phase == Phase.Chasing)
            return;

        _phase = Phase.Chasing;
        _patrol.SetPatrolActive(false);
        SetDrivenBySteering(true);
        _steering.SetSteeringActive(true, bypassRangeLimit: true);
    }

    void EnterInvestigate()
    {
        _phase = Phase.InvestigateMove;
        _waitAtLastSeen = 0f;
        _stuckTime = 0f;
        _closestDistance = float.MaxValue;
        _steering.SetSteeringActive(false);
        _patrol.SetPatrolActive(false);
        SetDrivenBySteering(false);

        if (_rb != null)
            transform.position = _rb.position;
    }

    void UpdateInvestigateMove()
    {
        Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;
        float distance = Vector2.Distance(pos, _lastSeenPosition);

        if (distance <= investigateArriveDistance || IsStuck(distance))
        {
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;

            _phase = Phase.InvestigateWait;
            _waitAtLastSeen = 0f;
            return;
        }

        AgentMove2D.MoveTowards(transform, _rb, _lastSeenPosition, investigateSpeed);
    }

    void UpdateInvestigateWait()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        _waitAtLastSeen += Time.fixedDeltaTime;
        if (_waitAtLastSeen < returnToPatrolDelay)
            return;

        _phase = Phase.Patrolling;
        SetDrivenBySteering(false);
        _patrol.SetPatrolActive(true);
    }

    bool IsStuck(float distance)
    {
        if (distance < _closestDistance - 0.02f)
        {
            _closestDistance = distance;
            _stuckTime = 0f;
            return false;
        }

        _stuckTime += Time.fixedDeltaTime;
        return _stuckTime > 0.75f;
    }

    void SetDrivenBySteering(bool steering)
    {
        if (_rb == null)
            return;

        _rb.bodyType = steering ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        _rb.linearVelocity = Vector2.zero;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    [SerializeField] bool showDetectionGizmo = true;
    [SerializeField] Color detectionGizmoColor = new Color(1f, 0.35f, 0.35f, 0.35f);
    [SerializeField] bool showLastSeenGizmo = true;
    [SerializeField] Color lastSeenGizmoColor = new Color(1f, 0.6f, 0.2f, 0.85f);

    void OnDrawGizmos()
    {
        Vector3 origin = Application.isPlaying && _rb != null ? (Vector3)_rb.position : transform.position;

        if (showDetectionGizmo)
        {
            Gizmos.color = detectionGizmoColor;
            Gizmos.DrawWireSphere(origin, detectionRadius);
        }

        if (!showLastSeenGizmo || _phase != Phase.InvestigateMove && _phase != Phase.InvestigateWait)
            return;

        Gizmos.color = lastSeenGizmoColor;
        Gizmos.DrawWireSphere(_lastSeenPosition, investigateArriveDistance);
        Gizmos.DrawLine(origin, _lastSeenPosition);
    }
}
