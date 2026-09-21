using UnityEngine;

/// <summary>
/// Simplest steering agent: no Rigidbody, no range checks.
/// Used by SeekAndFleeDemo. In-scene enemies use SteeringEnemy instead.
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
        Apply(SteeringMath.SeekVelocity(transform.position, target, maxSpeed));
    }

    public void FleeFrom(Vector2 threat)
    {
        Apply(SteeringMath.FleeVelocity(transform.position, threat, maxSpeed));
    }

    void Apply(Vector2 desiredVelocity)
    {
        _velocity = SteeringMath.ApplySteering(_velocity, desiredVelocity, maxForce, maxSpeed, Time.deltaTime);
        transform.position += (Vector3)(_velocity * Time.deltaTime);
    }
}
