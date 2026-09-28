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
    bool _active = true;

    public bool IsActive => _active;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void SetActive(bool active)
    {
        _active = active;
        if (!active || _rb == null)
            return;

        _rb.linearVelocity = Vector2.zero;
        transform.position = _rb.position;
    }

    void FixedUpdate()
    {
        if (!_active || waypoints == null || waypoints.Length == 0)
            return;

        _index = Mathf.Clamp(_index, 0, waypoints.Length - 1);
        for (int i = 0; i < waypoints.Length; i++)
        {
            Waypoint target = waypoints[_index];
            if (target == null)
            {
                _index = (_index + 1) % waypoints.Length;
                continue;
            }

            if (AgentMove2D.StepTowards(transform, _rb, target.Position, speed, arriveDistance))
                _index = (_index + 1) % waypoints.Length;
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = new Color(0.3f, 1f, 0.45f, 0.35f);
        for (int i = 0; i < waypoints.Length; i++)
        {
            Waypoint from = waypoints[i];
            Waypoint to = waypoints[(i + 1) % waypoints.Length];
            if (from == null || to == null)
                continue;

            Gizmos.DrawLine(from.Position, to.Position);
        }

        if (!_active)
            return;

        Waypoint target = waypoints[Mathf.Clamp(_index, 0, waypoints.Length - 1)];
        if (target == null)
            return;

        Vector3 origin = Application.isPlaying && _rb != null ? (Vector3)_rb.position : transform.position;
        Gizmos.color = new Color(0.3f, 1f, 0.45f, 0.9f);
        Gizmos.DrawLine(origin, target.Position);
        Gizmos.DrawWireSphere(target.Position, arriveDistance);
    }
}
