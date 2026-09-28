using UnityEngine;

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

    void Awake()
    {
        _patrol = GetComponent<WaypointPatrol>();
        _steering = GetComponent<SteeringEnemy>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        _steering.SetSteeringActive(false);
        _patrol.SetPatrolActive(true);
        _phase = Phase.Patrolling;
    }

    void Update()
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
        _phase = Phase.Chasing;
        _waitAtLastSeen = 0f;
        _patrol.SetPatrolActive(false);
        _steering.SetSteeringActive(true, bypassRangeLimit: true);
    }

    void EnterInvestigate()
    {
        _phase = Phase.InvestigateMove;
        _waitAtLastSeen = 0f;
        _steering.SetSteeringActive(false);
        _patrol.SetPatrolActive(false);

        if (_rb != null)
            transform.position = _rb.position;
    }

    void UpdateInvestigateMove()
    {
        Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;

        if (Vector2.Distance(pos, _lastSeenPosition) > investigateArriveDistance)
        {
            AgentMove2D.MoveTowards(transform, _rb, _lastSeenPosition, investigateSpeed);
            return;
        }

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        _phase = Phase.InvestigateWait;
        _waitAtLastSeen = 0f;
    }

    void UpdateInvestigateWait()
    {
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        _waitAtLastSeen += Time.deltaTime;
        if (_waitAtLastSeen < returnToPatrolDelay)
            return;

        _phase = Phase.Patrolling;
        _patrol.SetPatrolActive(true);
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    [SerializeField] bool showDetectionGizmo = true;
    [SerializeField] Color detectionGizmoColor = new Color(1f, 0.35f, 0.35f, 0.35f);
    [SerializeField] bool showLastSeenGizmo = true;
    [SerializeField] Color lastSeenGizmoColor = new Color(1f, 0.6f, 0.2f, 0.85f);

    void OnDrawGizmosSelected()
    {
        if (showDetectionGizmo)
        {
            Gizmos.color = detectionGizmoColor;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        if (!showLastSeenGizmo || _phase != Phase.InvestigateMove && _phase != Phase.InvestigateWait)
            return;

        Gizmos.color = lastSeenGizmoColor;
        Gizmos.DrawWireSphere(_lastSeenPosition, investigateArriveDistance);
        Gizmos.DrawLine(transform.position, _lastSeenPosition);
    }
}
