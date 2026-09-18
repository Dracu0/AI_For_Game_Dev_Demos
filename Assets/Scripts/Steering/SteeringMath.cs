// ReSharper disable CheckNamespace
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
/// Optional collision avoidance is added to steering before velocity integration.
/// </summary>
public static class SteeringMath
{
    public const float Epsilon = 0.0001f;

    static readonly Collider2D[] OverlapHits = new Collider2D[16];
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

        if (avoidance != null)
        {
            desiredVelocity = avoidance.SlideDesired(position, desiredVelocity);
            current = avoidance.StripIntoWalls(position, current);
        }

        Vector2 steering = Vector2.ClampMagnitude(desiredVelocity - current, maxForce);
        Vector2 velocity = current + steering * deltaTime;

        if (avoidance != null)
        {
            velocity += (avoidance.GetForce(position, current, desiredVelocity, maxSpeed)
                + avoidance.GetSeparationForce(position)) * deltaTime;
            velocity = avoidance.StripIntoWalls(position, velocity);
        }

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

    public static Vector2 CollisionAvoidance(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed,
        float seeAhead,
        float maxAvoidForce,
        float agentRadius,
        LayerMask obstacles,
        Collider2D ignore)
    {
        Vector2 forward = ResolveLookDirection(velocity, desiredVelocity);
        if (forward.sqrMagnitude < Epsilon)
            return Vector2.zero;

        float speed = Mathf.Max(velocity.magnitude, desiredVelocity.magnitude);
        float speedRatio = maxSpeed > Epsilon ? speed / maxSpeed : 1f;
        float lookDistance = seeAhead * Mathf.Clamp(speedRatio, 0.35f, 1f);
        Vector2 ahead = position + forward * lookDistance;
        Vector2 ahead2 = position + forward * lookDistance * 0.5f;

        if (!FindMostThreateningObstacle(
                position,
                ahead,
                ahead2,
                forward,
                lookDistance,
                agentRadius,
                obstacles,
                ignore,
                out Collider2D threat))
            return Vector2.zero;

        Vector2 wallPoint = threat.ClosestPoint(ahead);
        Vector2 avoidance = ahead - wallPoint;
        if (avoidance.sqrMagnitude < Epsilon)
            avoidance = position - threat.ClosestPoint(position);

        if (avoidance.sqrMagnitude < Epsilon)
            return Vector2.zero;

        float penetration = agentRadius - Vector2.Distance(position, threat.ClosestPoint(position));
        float urgency = 1f + Mathf.Clamp01(penetration / agentRadius);
        return avoidance.normalized * (maxAvoidForce * urgency);
    }

    public static Vector2 SlideDesiredAlongWalls(
        Vector2 position,
        Vector2 desiredVelocity,
        float agentRadius,
        LayerMask obstacles,
        Collider2D ignore) =>
        RemoveVelocityIntoWalls(position, desiredVelocity, agentRadius, obstacles, ignore);

    public static Vector2 WallSeparationForce(
        Vector2 position,
        float agentRadius,
        float maxForce,
        LayerMask obstacles,
        Collider2D ignore)
    {
        Vector2 separation = Vector2.zero;
        int count = OverlapCircle(position, agentRadius, obstacles, OverlapHits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = OverlapHits[i];
            if (hit == null || hit == ignore)
                continue;

            Vector2 closest = hit.ClosestPoint(position);
            Vector2 toAgent = position - closest;
            float distance = toAgent.magnitude;
            if (distance >= agentRadius)
                continue;

            float penetration = agentRadius - distance;
            Vector2 normal = distance > Epsilon ? toAgent / distance : Vector2.up;
            separation += normal * (penetration / agentRadius);
        }

        if (separation.sqrMagnitude < Epsilon)
            return Vector2.zero;

        return separation.normalized * maxForce;
    }

    public static Vector2 StripVelocityIntoWalls(
        Vector2 position,
        Vector2 velocity,
        float agentRadius,
        LayerMask obstacles,
        Collider2D ignore) =>
        RemoveVelocityIntoWalls(position, velocity, agentRadius, obstacles, ignore);

    static Vector2 RemoveVelocityIntoWalls(
        Vector2 position,
        Vector2 velocity,
        float agentRadius,
        LayerMask obstacles,
        Collider2D ignore)
    {
        if (velocity.sqrMagnitude < Epsilon)
            return velocity;

        Vector2 adjusted = velocity;
        int count = OverlapCircle(position, agentRadius * 1.02f, obstacles, OverlapHits);
        for (int i = 0; i < count; i++)
        {
            if (!TryGetWallNormal(position, OverlapHits[i], ignore, out Vector2 normal))
                continue;

            float intoWall = Vector2.Dot(adjusted, -normal);
            if (intoWall > 0f)
                adjusted += normal * intoWall;
        }

        return adjusted;
    }

    static bool TryGetWallNormal(Vector2 position, Collider2D wall, Collider2D ignore, out Vector2 normal)
    {
        normal = Vector2.zero;
        if (wall == null || wall == ignore)
            return false;

        Vector2 closest = wall.ClosestPoint(position);
        Vector2 toAgent = position - closest;
        if (toAgent.sqrMagnitude < Epsilon)
            return false;

        normal = toAgent.normalized;
        return true;
    }

    static Vector2 ResolveLookDirection(Vector2 velocity, Vector2 desiredVelocity)
    {
        if (velocity.sqrMagnitude > Epsilon)
            return velocity.normalized;

        if (desiredVelocity.sqrMagnitude > Epsilon)
            return desiredVelocity.normalized;

        return Vector2.zero;
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

    static bool FindMostThreateningObstacle(
        Vector2 position,
        Vector2 ahead,
        Vector2 ahead2,
        Vector2 forward,
        float lookDistance,
        float agentRadius,
        LayerMask obstacles,
        Collider2D ignore,
        out Collider2D threat)
    {
        Collider2D closest = null;
        float closestDistanceSq = float.MaxValue;

        ConsiderCast();
        ConsiderPoint(position);
        ConsiderPoint(ahead);
        ConsiderPoint(ahead2);

        threat = closest;
        return closest != null;

        void ConsiderCast()
        {
            RaycastHit2D hit = Physics2D.CircleCast(
                position,
                agentRadius * 0.95f,
                forward,
                lookDistance,
                obstacles);
            if (hit.collider == null || hit.collider == ignore)
                return;

            Register(hit.collider);
        }

        void ConsiderPoint(Vector2 point)
        {
            int count = OverlapCircle(point, agentRadius, obstacles, OverlapHits);
            for (int i = 0; i < count; i++)
                Register(OverlapHits[i]);
        }

        void Register(Collider2D hit)
        {
            if (hit == null || hit == ignore)
                return;

            float distanceSq = (hit.ClosestPoint(position) - position).sqrMagnitude;
            if (distanceSq < closestDistanceSq)
            {
                closestDistanceSq = distanceSq;
                closest = hit;
            }
        }
    }

    static int OverlapCircle(Vector2 point, float radius, LayerMask layers, Collider2D[] buffer)
    {
        ContactFilter2D filter = default;
        filter.useLayerMask = true;
        filter.SetLayerMask(layers);
        filter.useTriggers = false;
        return Physics2D.OverlapCircle(point, radius, filter, buffer);
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
