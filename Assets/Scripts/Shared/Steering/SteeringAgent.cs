using UnityEngine;

/// <summary>
/// Simple steering agent used by the Seek vs Flee demo.
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
        Vector2 toTarget = target - (Vector2)transform.position;
        if (toTarget.sqrMagnitude < 0.0001f)
            return;

        Vector2 desiredVelocity = toTarget.normalized * maxSpeed;
        Vector2 steering = Vector2.ClampMagnitude(desiredVelocity - _velocity, maxForce);
        ApplySteering(steering);
    }

    public void FleeFrom(Vector2 threat)
    {
        Vector2 awayFromThreat = (Vector2)transform.position - threat;
        if (awayFromThreat.sqrMagnitude < 0.0001f)
            return;

        Vector2 desiredVelocity = awayFromThreat.normalized * maxSpeed;
        Vector2 steering = Vector2.ClampMagnitude(desiredVelocity - _velocity, maxForce);
        ApplySteering(steering);
    }

    void ApplySteering(Vector2 steering)
    {
        _velocity += steering * Time.deltaTime;
        _velocity = Vector2.ClampMagnitude(_velocity, maxSpeed);
        transform.position += (Vector3)(_velocity * Time.deltaTime);
    }
}
