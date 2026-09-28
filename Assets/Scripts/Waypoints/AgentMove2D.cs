using UnityEngine;

/// <summary>
/// Kinematic-style moves that keep <see cref="Rigidbody2D"/> and transform in sync.
/// </summary>
public static class AgentMove2D
{
    public static void MoveTowards(Transform transform, Rigidbody2D rb, Vector2 goal, float speed)
    {
        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 next = Vector2.MoveTowards(pos, goal, speed * Time.deltaTime);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.MovePosition(next);
        }
        else
            transform.position = next;
    }
}
