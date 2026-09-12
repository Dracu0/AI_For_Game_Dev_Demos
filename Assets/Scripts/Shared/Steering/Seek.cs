using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Seek steers an agent toward a target at maximum speed.
    /// desired velocity = (target - position).normalized * maxSpeed
    /// steering force   = desired velocity - current velocity
    /// </summary>
    public static class SeekBehaviour
    {
        public static Vector2 Calculate(
            Vector2 position,
            Vector2 velocity,
            Vector2 target,
            float maxSpeed,
            float maxForce)
        {
            Vector2 toTarget = target - position;
            if (toTarget.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            Vector2 desiredVelocity = toTarget.normalized * maxSpeed;
            Vector2 steering = desiredVelocity - velocity;
            return Vector2.ClampMagnitude(steering, maxForce);
        }
    }
}
