using UnityEngine;

/// <summary>
/// Shared steering formula used by Seek, Flee, Arrival, and Pursue.
///
///   steering = desiredVelocity - currentVelocity
///   velocity += steering * deltaTime
///
/// Each enemy script only decides what desiredVelocity should be.
/// </summary>
public static class SteeringMath
{
    public const float Epsilon = 0.0001f;

    public static Vector2 Direction(Vector2 from, Vector2 to)
    {
        Vector2 offset = to - from;
        if (offset.sqrMagnitude < Epsilon)
            return Vector2.zero;

        return offset.normalized;
    }

    public static Vector2 Steer(
        Rigidbody2D rb,
        Vector2 desiredVelocity,
        float maxForce,
        float maxSpeed)
    {
        Vector2 steering = Vector2.ClampMagnitude(desiredVelocity - rb.linearVelocity, maxForce);
        Vector2 velocity = rb.linearVelocity + steering * Time.fixedDeltaTime;
        return Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    public static void Stop(Rigidbody2D rb)
    {
        rb.linearVelocity = Vector2.zero;
    }

    public static void ResizeCircle(Transform circle, float radius)
    {
        if (circle == null)
            return;

        SpriteRenderer sprite = circle.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return;

        float diameter = sprite.sprite.bounds.size.x;
        if (diameter <= 0f)
            return;

        circle.localScale = Vector3.one * (radius * 2f / diameter);
    }
}
