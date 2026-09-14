using UnityEngine;

/// <summary>
/// Shared steering math used by every steering behaviour.
///
/// Core formula (same in every behaviour):
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

    /// <summary>Apply the steering formula to a velocity value.</summary>
    public static Vector2 ApplySteering(
        Vector2 currentVelocity,
        Vector2 desiredVelocity,
        float maxForce,
        float maxSpeed,
        float deltaTime)
    {
        Vector2 steering = Vector2.ClampMagnitude(desiredVelocity - currentVelocity, maxForce);
        return Vector2.ClampMagnitude(currentVelocity + steering * deltaTime, maxSpeed);
    }

    /// <summary>Apply steering through a Rigidbody2D (used by enemy scripts).</summary>
    public static Vector2 Steer(
        Rigidbody2D rb,
        Vector2 desiredVelocity,
        float maxForce,
        float maxSpeed)
    {
        return ApplySteering(rb.linearVelocity, desiredVelocity, maxForce, maxSpeed, Time.fixedDeltaTime);
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
