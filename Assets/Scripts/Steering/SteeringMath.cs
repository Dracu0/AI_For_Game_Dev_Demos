using UnityEngine;

/// <summary>
/// Shared steering math used by every steering behaviour.
///
/// Core formula (same in every behaviour):
///   steering = desiredVelocity - currentVelocity
///   velocity += steering * deltaTime
///
/// Behaviour scripts pick desiredVelocity using SeekVelocity, FleeVelocity,
/// ArrivalVelocity, or PredictPosition, then apply it with Steer.
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

    public static void SetupEnemy(Rigidbody2D rb)
    {
        rb.gravityScale = 0f;
    }

    public static void Stop(Rigidbody2D rb)
    {
        rb.linearVelocity = Vector2.zero;
    }

    public static bool IsOutOfRange(Vector2 from, Vector2 to, float range) =>
        Vector2.Distance(from, to) > range;

    public static Vector2 SeekVelocity(Vector2 from, Vector2 to, float maxSpeed) =>
        Direction(from, to) * maxSpeed;

    public static Vector2 FleeVelocity(Vector2 from, Vector2 threat, float maxSpeed) =>
        Direction(threat, from) * maxSpeed;

    public static Vector2 ArrivalVelocity(
        Vector2 from,
        Vector2 to,
        float distance,
        float maxSpeed,
        float slowRadius,
        float stoppingDistance)
    {
        if (distance < stoppingDistance)
            return Vector2.zero;

        Vector2 direction = Direction(from, to);
        if (distance < slowRadius)
            return direction * (maxSpeed * (distance / slowRadius));

        return direction * maxSpeed;
    }

    public static Vector2 PredictPosition(
        Vector2 chaserPosition,
        Vector2 targetPosition,
        Vector2 targetVelocity,
        float maxSpeed)
    {
        if (targetVelocity.sqrMagnitude < Epsilon)
            return targetPosition;

        float distance = Vector2.Distance(chaserPosition, targetPosition);
        float lookAheadTime = distance / maxSpeed;
        return targetPosition + targetVelocity * lookAheadTime;
    }

    public static void SteerIfMoving(
        Rigidbody2D rb,
        Vector2 desiredVelocity,
        float maxForce,
        float maxSpeed)
    {
        if (desiredVelocity.sqrMagnitude < Epsilon)
            return;

        rb.linearVelocity = Steer(rb, desiredVelocity, maxForce, maxSpeed);
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
