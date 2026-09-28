using UnityEngine;

[RequireComponent(typeof(WaypointPatrol), typeof(SteeringEnemy))]
public class WaypointPatrolChase : MonoBehaviour
{
    [SerializeField] float detectionRadius = 5f;
    [SerializeField] float returnToPatrolDelay = 2f;

    WaypointPatrol _patrol;
    SteeringEnemy _steering;
    float _wait;

    void Awake()
    {
        _patrol = GetComponent<WaypointPatrol>();
        _steering = GetComponent<SteeringEnemy>();
    }

    void Start() => _steering.SetSteeringActive(false);

    void Update()
    {
        Transform player = _steering.Player;
        if (player == null)
            return;

        if (Vector2.Distance(transform.position, player.position) <= detectionRadius)
        {
            _wait = 0f;
            _patrol.SetPatrolActive(false);
            _steering.SetSteeringActive(true);
            return;
        }

        if (_steering.IsSteeringActive)
        {
            _steering.SetSteeringActive(false);
            _wait = 0f;
        }

        if (_patrol.IsPatrolActive)
            return;

        _wait += Time.deltaTime;
        if (_wait >= returnToPatrolDelay)
            _patrol.SetPatrolActive(true);
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    [SerializeField] bool showDetectionGizmo = true;
    [SerializeField] Color detectionGizmoColor = new Color(1f, 0.35f, 0.35f, 0.35f);

    void OnDrawGizmosSelected()
    {
        if (!showDetectionGizmo)
            return;

        Gizmos.color = detectionGizmoColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
