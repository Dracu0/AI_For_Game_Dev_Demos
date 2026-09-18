using UnityEngine;

/// <summary>
/// Ahead / ahead2 collision check against static obstacles (Tuts+ collision avoidance).
/// Add to enemies that should steer around the Obstacle layer.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class SteeringCollisionAvoidance : MonoBehaviour
{
    [SerializeField] LayerMask obstacleLayers = 1 << 3;
    [SerializeField] float seeAhead = 2f;
    [SerializeField] float maxAvoidForce = 6f;

    CircleCollider2D _circle;

    void Awake()
    {
        _circle = GetComponent<CircleCollider2D>();
    }

    public Vector2 GetForce(Vector2 position, Vector2 velocity, float maxSpeed)
    {
        float radius = _circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        return SteeringMath.CollisionAvoidance(
            position,
            velocity,
            maxSpeed,
            seeAhead,
            maxAvoidForce,
            radius,
            obstacleLayers,
            _circle);
    }
}
