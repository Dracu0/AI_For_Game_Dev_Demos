// ReSharper disable CheckNamespace
using UnityEngine;

/// <summary>Shared steering helpers: desired velocity, integration, optional collision avoidance.</summary>
public static class SteeringMath
{
    public const float Epsilon = 0.0001f;

    static PhysicsMaterial2D _frictionlessMaterial;

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
        float maxSpeed,
        SteeringCollisionAvoidance avoidance)
    {
        Vector2 position = rb.position;
        Vector2 current = rb.linearVelocity;
        float deltaTime = Time.fixedDeltaTime;

        Vector2 avoidanceForce = avoidance != null
            ? avoidance.GetAvoidanceForce(position, current, desiredVelocity, maxSpeed)
            : Vector2.zero;

        Vector2 steering = Vector2.ClampMagnitude(
            desiredVelocity - current + avoidanceForce,
            maxForce);
        Vector2 velocity = current + steering * deltaTime;

        if (avoidance != null)
            velocity = avoidance.PreventMovingIntoWalls(position, velocity);

        return Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    public static void SetupEnemy(Rigidbody2D rb)
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (_frictionlessMaterial == null)
            _frictionlessMaterial = new PhysicsMaterial2D { friction = 0f, bounciness = 0f };

        rb.sharedMaterial = _frictionlessMaterial;
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
        -SeekVelocity(from, threat, maxSpeed);

    public static Vector2 ArrivalVelocity(
        Vector2 from,
        Vector2 to,
        float distance,
        float maxSpeed,
        float slowRadius)
    {
        Vector2 direction = Direction(from, to);
        if (distance < slowRadius)
            return direction * (maxSpeed * (distance / slowRadius));

        return direction * maxSpeed;
    }

    public static Vector2 PredictPosition(
        Vector2 agentPosition,
        Vector2 targetPosition,
        Vector2 targetVelocity,
        float targetMaxSpeed)
    {
        if (targetMaxSpeed < Epsilon)
            return targetPosition;

        float distance = Vector2.Distance(agentPosition, targetPosition);
        float lookAheadTime = distance / targetMaxSpeed;
        return targetPosition + targetVelocity * lookAheadTime;
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
