using UnityEngine;

[RequireComponent(typeof(SteeringEnemy))]
public class WaypointPatrol : MonoBehaviour
{
    [SerializeField] Waypoint[] waypoints;
    [SerializeField] float speed = 3f;

    SteeringEnemy _steering;
    int _index;
    bool _patrolActive = true;

    public bool IsPatrolActive => _patrolActive;

    void Awake() => _steering = GetComponent<SteeringEnemy>();

    public void SetPatrolActive(bool active)
    {
        _patrolActive = active;
        if (!active)
            return;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            transform.position = rb.position;
    }

    void Update()
    {
        if (!_patrolActive || waypoints == null || waypoints.Length == 0)
            return;

        Waypoint target = waypoints[_index];
        if (target == null)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.transform.position,
            speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.transform.position) < 0.05f)
            _index = (_index + 1) % waypoints.Length;
    }
}
