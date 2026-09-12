using UnityEngine;

/// <summary>
/// Enemy moves toward the player, slows down near them, then stops.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ArrivalEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Arrival")]
    [SerializeField] Transform slowRadiusCircle;
    [SerializeField] float slowRadius = 3f;
    [SerializeField] float stoppingDistance = 0.25f;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 3f;
    [SerializeField] float maxForce = 6f;

    Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        UpdateRangeCircle();
    }

    void OnValidate()
    {
        UpdateRangeCircle();
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        Vector2 toPlayer = (Vector2)player.position - _rb.position;
        float distance = toPlayer.magnitude;

        Vector2 desiredVelocity;
        if (distance < stoppingDistance)
        {
            desiredVelocity = Vector2.zero;
        }
        else if (distance < slowRadius)
        {
            desiredVelocity = toPlayer / distance * (maxSpeed * (distance / slowRadius));
        }
        else
        {
            desiredVelocity = toPlayer / distance * maxSpeed;
        }

        Vector2 steering = Vector2.ClampMagnitude(
            desiredVelocity - _rb.linearVelocity,
            maxForce);

        Vector2 velocity = _rb.linearVelocity + steering * Time.fixedDeltaTime;
        _rb.linearVelocity = Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    void UpdateRangeCircle()
    {
        if (slowRadiusCircle == null)
            return;

        SpriteRenderer sprite = slowRadiusCircle.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return;

        float diameter = sprite.sprite.bounds.size.x;
        if (diameter <= 0f)
            return;

        slowRadiusCircle.localScale = Vector3.one * (slowRadius * 2f / diameter);
    }
}
