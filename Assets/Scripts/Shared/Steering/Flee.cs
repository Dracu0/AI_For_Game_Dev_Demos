using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Flee steers an agent away from a threat at maximum speed.
    /// desired velocity = (position - threat).normalized * maxSpeed
    /// steering force   = desired velocity - current velocity
    /// </summary>
    public static class Flee
    {
        public static Vector2 Calculate(
            Vector2 position,
            Vector2 velocity,
            Vector2 threat,
            float maxSpeed,
            float maxForce)
        {
            Vector2 awayFromThreat = position - threat;
            if (awayFromThreat.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            Vector2 desiredVelocity = awayFromThreat.normalized * maxSpeed;
            Vector2 steering = desiredVelocity - velocity;
            return Vector2.ClampMagnitude(steering, maxForce);
        }
    }
}
