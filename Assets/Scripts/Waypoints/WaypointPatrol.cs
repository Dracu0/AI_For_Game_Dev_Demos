using UnityEngine;

[RequireComponent(typeof(SteeringEnemy))]
[DefaultExecutionOrder(10)]
public class WaypointPatrol : MonoBehaviour
{
    [SerializeField] Waypoint[] waypoints;
    [SerializeField] float speed = 3f;

    Rigidbody2D _rb;
    int _index;
    bool _patrolActive = true;

    public bool IsPatrolActive => _patrolActive;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void SetPatrolActive(bool active)
    {
        _patrolActive = active;
        if (!active)
            return;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = _rb.position;
        }
    }

    void Update()
    {
        if (!_patrolActive || waypoints == null || waypoints.Length == 0)
            return;

        Waypoint target = waypoints[_index];
        if (target == null)
            return;

        AgentMove2D.MoveTowards(transform, _rb, target.Position, speed);

        Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;
        if (Vector2.Distance(pos, target.Position) < 0.05f)
            _index = (_index + 1) % waypoints.Length;
    }
}
