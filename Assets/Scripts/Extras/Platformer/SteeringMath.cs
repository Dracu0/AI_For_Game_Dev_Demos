using UnityEngine;

/// <summary>
/// Shared steering formulas. SteeringEnemy picks a desired velocity;
/// this class turns that into a new velocity (and optional avoidance).
/// </summary>
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

    /// <summary>
    /// steering = clamp(desired - velocity, maxForce)
    /// velocity = clamp(velocity + steering * dt, maxSpeed)
    /// goalDistance lets avoidance ignore walls that are behind the goal.
    /// </summary>
    public static Vector2 Steer(
        Rigidbody2D rb,
        Vector2 desiredVelocity,
        float maxForce,
        float maxSpeed,
        SteeringCollisionAvoidance avoidance,
        float goalDistance = float.PositiveInfinity)
    {
        Vector2 position = rb.position;
        Vector2 current = rb.linearVelocity;
        float deltaTime = Time.fixedDeltaTime;

        if (avoidance != null)
            desiredVelocity = avoidance.ResolveDesired(position, desiredVelocity, goalDistance);

        Vector2 steering = Vector2.ClampMagnitude(desiredVelocity - current, maxForce);
        Vector2 velocity = current + steering * deltaTime;

        if (avoidance != null)
            velocity = avoidance.PreventMovingIntoWalls(position, velocity);

        return Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    public static void SetupBody(Rigidbody2D rb)
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

    public static Vector2 SeekVelocity(Vector2 from, Vector2 to, float maxSpeed) =>
        Direction(from, to) * maxSpeed;

    public static Vector2 FleeVelocity(Vector2 from, Vector2 threat, float maxSpeed) =>
        -SeekVelocity(from, threat, maxSpeed);

    public static Vector2 ArrivalVelocity(Vector2 from, Vector2 to, float maxSpeed, float slowRadius)
    {
        Vector2 offset = to - from;
        float distance = offset.magnitude;
        if (distance < Epsilon)
            return Vector2.zero;

        Vector2 direction = offset / distance;
        if (slowRadius > Epsilon && distance < slowRadius)
            return direction * (maxSpeed * (distance / slowRadius));

        return direction * maxSpeed;
    }

    /// <summary>
    /// Where the target will be by the time the AGENT can get there
    /// (time = distance / agent speed, capped so far-away targets aren't over-led).
    /// </summary>
    public static Vector2 PredictPosition(
        Vector2 agentPosition,
        Vector2 targetPosition,
        Vector2 targetVelocity,
        float agentMaxSpeed,
        float maxPredictionTime)
    {
        if (agentMaxSpeed < Epsilon)
            return targetPosition;

        float distance = Vector2.Distance(agentPosition, targetPosition);
        float lookAheadTime = Mathf.Min(distance / agentMaxSpeed, Mathf.Max(0f, maxPredictionTime));
        return targetPosition + targetVelocity * lookAheadTime;
    }
}
