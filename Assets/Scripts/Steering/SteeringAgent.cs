using UnityEngine;

/// <summary>
/// Simple agent used by the Seek vs Flee demo.
/// Uses the same SteeringMath formula as the enemy scripts,
/// but moves a Transform directly instead of a Rigidbody2D.
/// </summary>
public class SteeringAgent : MonoBehaviour
{
    [SerializeField] float maxSpeed = 4f;
    [SerializeField] float maxForce = 8f;

    Vector2 _velocity;

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
        Apply(SteeringMath.Direction(transform.position, target) * maxSpeed);
    }

    public void FleeFrom(Vector2 threat)
    {
        Apply(SteeringMath.Direction(threat, transform.position) * maxSpeed);
    }

    void Apply(Vector2 desiredVelocity)
    {
        _velocity = SteeringMath.ApplySteering(_velocity, desiredVelocity, maxForce, maxSpeed, Time.deltaTime);
        transform.position += (Vector3)(_velocity * Time.deltaTime);
    }
}
