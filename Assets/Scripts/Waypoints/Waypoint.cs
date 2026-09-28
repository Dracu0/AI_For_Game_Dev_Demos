using UnityEngine;

/// <summary>
/// One point on the map. Connections are only drawn; patrol order lives on WaypointPatrol.
/// </summary>
public class Waypoint : MonoBehaviour
{
    [SerializeField] Waypoint[] connections;
    [SerializeField] float nodeRadius = 0.2f;

    public Vector2 Position => transform.position;

    // -------------------------------------------------------------------------
    // Gizmos
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.95f);
        Gizmos.DrawWireSphere(transform.position, nodeRadius);

        if (connections == null)
            return;

        Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.65f);
        foreach (Waypoint other in connections)
        {
            if (other == null || other == this)
                continue;

            Gizmos.DrawLine(transform.position, other.Position);
        }
    }
}
