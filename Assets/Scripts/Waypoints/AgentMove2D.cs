using UnityEngine;

/// <summary>
/// Straight-line move that stays in sync with a Rigidbody2D. Call from FixedUpdate.
/// </summary>
public static class AgentMove2D
{
    public static void MoveTowards(Transform transform, Rigidbody2D rb, Vector2 goal, float speed)
    {
        float step = speed * Time.fixedDeltaTime;
        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 next = Vector2.MoveTowards(pos, goal, step);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.MovePosition(next);
        }
        else
            transform.position = next;
    }
}
