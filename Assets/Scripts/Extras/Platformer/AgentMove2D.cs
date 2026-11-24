using UnityEngine;

/// <summary>
/// Moves in a straight line. Returns true once it is close enough. Call from FixedUpdate.
/// </summary>
public static class AgentMove2D
{
    public static bool StepTowards(Transform transform, Rigidbody2D rb, Vector2 goal, float speed, float arriveDistance)
    {
        Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
        if (Vector2.Distance(pos, goal) <= arriveDistance)
            return true;

        Vector2 next = Vector2.MoveTowards(pos, goal, speed * Time.fixedDeltaTime);
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.MovePosition(next);
        }
        else
            transform.position = next;

        return false;
    }
}
