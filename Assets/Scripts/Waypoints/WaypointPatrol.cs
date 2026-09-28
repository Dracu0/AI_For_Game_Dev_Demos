using UnityEngine;

public class WaypointPatrol : MonoBehaviour
{
    [SerializeField] Waypoint[] waypoints;
    [SerializeField] float speed = 3f;

    int _index;

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
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
