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
/// Optional collision avoidance is added to desired velocity before Steer.
/// </summary>
public static class SteeringMath
{
    public const float Epsilon = 0.0001f;

    static readonly Collider2D[] OverlapHits = new Collider2D[16];

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
        Vector2 extraVelocity = default)
    {
        return ApplySteering(
            rb.linearVelocity,
            desiredVelocity + extraVelocity,
            maxForce,
            maxSpeed,
            Time.fixedDeltaTime);
    }

    public static Vector2 Steer(
        Rigidbody2D rb,
        Vector2 desiredVelocity,
        float maxForce,
        float maxSpeed,
        SteeringCollisionAvoidance avoidance)
    {
        Vector2 extra = avoidance != null
            ? avoidance.GetForce(rb.position, rb.linearVelocity, maxSpeed)
            : Vector2.zero;

        return Steer(rb, desiredVelocity, maxForce, maxSpeed, extra);
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

    public static Vector2 CollisionAvoidance(
        Vector2 position,
        Vector2 velocity,
        float maxSpeed,
        float seeAhead,
        float maxAvoidForce,
        float agentRadius,
        LayerMask obstacles,
        Collider2D ignore)
    {
        if (velocity.sqrMagnitude < Epsilon)
            return Vector2.zero;

        float speed = velocity.magnitude;
        Vector2 forward = velocity / speed;
        float lookDistance = seeAhead * (speed / maxSpeed);
        Vector2 ahead = position + forward * lookDistance;
        Vector2 ahead2 = position + forward * lookDistance * 0.5f;

        Collider2D threat = FindMostThreateningObstacle(
            position,
            ahead,
            ahead2,
            agentRadius,
            obstacles,
            ignore);
        if (threat == null)
            return Vector2.zero;

        Vector2 avoidance = ahead - (Vector2)threat.bounds.center;
        if (avoidance.sqrMagnitude < Epsilon)
            return Vector2.zero;

        return avoidance.normalized * maxAvoidForce;
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

    static Collider2D FindMostThreateningObstacle(
        Vector2 position,
        Vector2 ahead,
        Vector2 ahead2,
        float agentRadius,
        LayerMask obstacles,
        Collider2D ignore)
    {
        Collider2D closest = null;
        float closestDistanceSq = float.MaxValue;

        ConsiderPoint(position, ref closest, ref closestDistanceSq);
        ConsiderPoint(ahead, ref closest, ref closestDistanceSq);
        ConsiderPoint(ahead2, ref closest, ref closestDistanceSq);

        return closest;

        void ConsiderPoint(Vector2 point, ref Collider2D best, ref float bestDistanceSq)
        {
            int count = Physics2D.OverlapCircleNonAlloc(point, agentRadius, OverlapHits, obstacles);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = OverlapHits[i];
                if (hit == null || hit == ignore)
                    continue;

                float distanceSq = (hit.ClosestPoint(position) - position).sqrMagnitude;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    best = hit;
                }
            }
        }
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
