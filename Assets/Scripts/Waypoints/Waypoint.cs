using UnityEngine;

/// <summary>
/// A single patrol node in 2D. Position is this transform.
/// Link other waypoints in <see cref="connections"/> to draw edges in the Scene view.
/// </summary>
public class Waypoint : MonoBehaviour
{
    [SerializeField] Waypoint[] connections;

    [Header("Gizmos")]
    [SerializeField] Color nodeColor = new Color(0.25f, 0.85f, 1f, 0.95f);
    [SerializeField] Color lineColor = new Color(0.25f, 0.85f, 1f, 0.65f);
    [SerializeField] float nodeRadius = 0.2f;

    public Vector2 Position => transform.position;

    void OnDrawGizmos()
    {
        Vector3 pos = transform.position;

        Gizmos.color = nodeColor;
        Gizmos.DrawWireSphere(pos, nodeRadius);

        if (connections == null || connections.Length == 0)
            return;

        Gizmos.color = lineColor;
        foreach (Waypoint other in connections)
        {
            if (other == null || other == this)
                continue;

            Gizmos.DrawLine(pos, other.transform.position);
        }
    }
}
