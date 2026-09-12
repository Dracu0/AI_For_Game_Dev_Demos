using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Reusable agent that stores velocity and applies steering forces each frame.
    /// Used by Seek, Flee, and future steering demos.
    /// </summary>
    public class SteeringAgent : MonoBehaviour
    {
        [SerializeField] float maxSpeed = 4f;
        [SerializeField] float maxForce = 8f;

        Vector2 _velocity;

        public Vector2 Position => transform.position;
        public Vector2 Velocity => _velocity;
        public float MaxSpeed => maxSpeed;
        public float MaxForce => maxForce;

        public void Configure(float speed, float force)
        {
            maxSpeed = speed;
            maxForce = force;
        }

        public void ResetMotion()
        {
            _velocity = Vector2.zero;
        }

        public void SeekToward(Vector2 target)
        {
            ApplySteering(Seek.Calculate(Position, _velocity, target, maxSpeed, maxForce));
        }

        public void FleeFrom(Vector2 threat)
        {
            ApplySteering(Flee.Calculate(Position, _velocity, threat, maxSpeed, maxForce));
        }

        void ApplySteering(Vector2 steeringForce)
        {
            _velocity += steeringForce * Time.deltaTime;
            _velocity = Vector2.ClampMagnitude(_velocity, maxSpeed);
            transform.position += (Vector3)(_velocity * Time.deltaTime);
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _velocity);
        }
    }
}
