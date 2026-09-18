using UnityEngine;

/// <summary>
/// Seek — desiredVelocity = normalize(target - position) * maxSpeed.
/// Only active inside seek range.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SeekEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Seek Range")]
    [SerializeField] Transform seekRangeCircle;
    [SerializeField] float seekRange = 5f;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        SteeringMath.SetupEnemy(_rb);
        SteeringMath.ResizeCircle(seekRangeCircle, seekRange);
    }

    void OnValidate()
    {
        SteeringMath.ResizeCircle(seekRangeCircle, seekRange);
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        if (SteeringMath.IsOutOfRange(_rb.position, player.position, seekRange))
        {
            SteeringMath.Stop(_rb);
            return;
        }

        _rb.linearVelocity = SteeringMath.Steer(
            _rb,
            SteeringMath.SeekVelocity(_rb.position, player.position, maxSpeed),
            maxForce,
            maxSpeed);
    }
}
