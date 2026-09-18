using UnityEngine;

/// <summary>
/// Arrival — seek at full speed, then ramp down inside slowRadius (distance / slowRadius).
/// Only active inside detection range.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArrivalEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Detection")]
    [SerializeField] Transform detectionRangeCircle;
    [SerializeField] float detectionRange = 5f;

    [Header("Arrival")]
    [SerializeField] Transform slowRadiusCircle;
    [SerializeField] float slowRadius = 3f;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        SteeringMath.SetupEnemy(_rb);
        UpdateRangeCircles();
    }

    void OnValidate()
    {
        UpdateRangeCircles();
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(_rb.position, player.position);
        if (distance > detectionRange)
        {
            SteeringMath.Stop(_rb);
            return;
        }

        Vector2 desiredVelocity = SteeringMath.ArrivalVelocity(
            _rb.position,
            player.position,
            distance,
            maxSpeed,
            slowRadius);
        _rb.linearVelocity = SteeringMath.Steer(_rb, desiredVelocity, maxForce, maxSpeed);
    }

    void UpdateRangeCircles()
    {
        SteeringMath.ResizeCircle(detectionRangeCircle, detectionRange);
        SteeringMath.ResizeCircle(slowRadiusCircle, slowRadius);
    }
}
