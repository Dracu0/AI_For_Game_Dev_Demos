using UnityEngine;

/// <summary>
/// Simple agent used by the Seek vs Flee demo.
/// Applies seek or flee using transform movement instead of a Rigidbody2D.
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
        Vector2 steering = Vector2.ClampMagnitude(desiredVelocity - _velocity, maxForce);
        _velocity += steering * Time.deltaTime;
        _velocity = Vector2.ClampMagnitude(_velocity, maxSpeed);
        transform.position += (Vector3)(_velocity * Time.deltaTime);
    }
}
