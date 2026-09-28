using UnityEngine;

/// <summary>
/// Walks an ordered list of waypoints and loops.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[DefaultExecutionOrder(10)]
public class WaypointPatrol : MonoBehaviour
{
    [SerializeField] Waypoint[] waypoints;
    [SerializeField] float speed = 3f;
    [SerializeField] float arriveDistance = 0.05f;

    Rigidbody2D _rb;
    int _index;
    bool _active = true;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void SetActive(bool active)
    {
        _active = active;
        if (!active || _rb == null)
            return;

        _rb.linearVelocity = Vector2.zero;
        transform.position = _rb.position;
        _index = ClosestIndex();
    }

    int ClosestIndex()
    {
        int closest = 0;
        float best = float.MaxValue;
        Vector2 pos = _rb != null ? _rb.position : (Vector2)transform.position;

        for (int i = 0; waypoints != null && i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            float distance = Vector2.Distance(pos, waypoints[i].Position);
            if (distance >= best)
                continue;

            best = distance;
            closest = i;
        }

        return closest;
    }

    void FixedUpdate()
    {
        if (!_active || waypoints == null || waypoints.Length == 0)
            return;

        Waypoint target = waypoints[_index];
        if (target == null)
        {
            _index = (_index + 1) % waypoints.Length;
            return;
        }

        if (AgentMove2D.StepTowards(transform, _rb, target.Position, speed, arriveDistance))
            _index = (_index + 1) % waypoints.Length;
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

        Waypoint current = waypoints[_index];
        if (current == null)
            return;

        Vector3 origin = transform.position;
        Gizmos.color = new Color(0.3f, 1f, 0.45f, 0.9f);
        Gizmos.DrawLine(origin, current.Position);
        Gizmos.DrawWireSphere(current.Position, arriveDistance);
    }
}
