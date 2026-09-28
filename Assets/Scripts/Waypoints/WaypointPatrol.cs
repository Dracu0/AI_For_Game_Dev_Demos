using UnityEngine;

[RequireComponent(typeof(SteeringEnemy))]
[DefaultExecutionOrder(10)]
public class WaypointPatrol : MonoBehaviour
{
    [SerializeField] Waypoint[] waypoints;
    [SerializeField] float speed = 3f;
    [SerializeField] float arriveDistance = 0.05f;

    Rigidbody2D _rb;
    int _index;
    bool _patrolActive = true;
    float _stuckTime;
    float _closestDistance;

    public bool IsPatrolActive => _patrolActive;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void SetPatrolActive(bool active)
    {
        if (_patrolActive == active)
            return;

        _patrolActive = active;
        _stuckTime = 0f;
        _closestDistance = float.MaxValue;

        if (!active || _rb == null)
            return;

        _rb.linearVelocity = Vector2.zero;
        transform.position = _rb.position;
    }

    void FixedUpdate()
    {
        if (!_patrolActive || waypoints == null || waypoints.Length == 0)
            return;

        Waypoint target = waypoints[_index];
        if (target == null)
        {
            Advance();
            return;
        }

        Vector2 goal = target.Position;
        Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;
        float distance = Vector2.Distance(pos, goal);

        if (distance <= arriveDistance || IsStuck(distance))
        {
            Advance();
            return;
        }

        AgentMove2D.MoveTowards(transform, _rb, goal, speed);
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

    void Advance()
    {
        _index = (_index + 1) % waypoints.Length;
        _stuckTime = 0f;
        _closestDistance = float.MaxValue;
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (!_patrolActive || waypoints == null || waypoints.Length == 0)
            return;

        int index = Mathf.Clamp(_index, 0, waypoints.Length - 1);
        Waypoint target = waypoints[index];
        if (target == null)
            return;

        Vector3 origin = Application.isPlaying && _rb != null ? (Vector3)_rb.position : transform.position;
        Gizmos.color = new Color(0.3f, 1f, 0.45f, 0.9f);
        Gizmos.DrawLine(origin, target.transform.position);
        Gizmos.DrawWireSphere(target.transform.position, arriveDistance);
    }
}
