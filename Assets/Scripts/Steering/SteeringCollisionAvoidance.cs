// ReSharper disable CheckNamespace
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Ahead / ahead2 collision check against static obstacles (Tuts+ collision avoidance).
/// Add to enemies that should steer around the Obstacle layer.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    [FormerlySerializedAs("obstacleLayers")]
    [SerializeField] LayerMask _obstacleLayers = 1 << 3;

    [FormerlySerializedAs("seeAhead")]
    [SerializeField] float _seeAhead = 3f;

    [FormerlySerializedAs("maxAvoidForce")]
    [SerializeField] float _maxAvoidForce = 12f;

    CircleCollider2D _circle;

    void Awake()
    {
        _circle = GetComponent<CircleCollider2D>();
    }

    public Vector2 SlideDesired(Vector2 position, Vector2 desiredVelocity) =>
        SteeringMath.SlideDesiredAlongWalls(position, desiredVelocity, AgentRadius(), _obstacleLayers, _circle);

    public Vector2 GetForce(
        Vector2 position,
        Vector2 velocity,
        Vector2 desiredVelocity,
        float maxSpeed) =>
        SteeringMath.CollisionAvoidance(
            position,
            velocity,
            desiredVelocity,
            maxSpeed,
            _seeAhead,
            _maxAvoidForce,
            AgentRadius(),
            _obstacleLayers,
            _circle);

    public Vector2 GetSeparationForce(Vector2 position) =>
        SteeringMath.WallSeparationForce(position, AgentRadius(), _maxAvoidForce, _obstacleLayers, _circle);

    public Vector2 StripIntoWalls(Vector2 position, Vector2 velocity) =>
        SteeringMath.StripVelocityIntoWalls(position, velocity, AgentRadius(), _obstacleLayers, _circle);

    float AgentRadius() =>
        _circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
}
