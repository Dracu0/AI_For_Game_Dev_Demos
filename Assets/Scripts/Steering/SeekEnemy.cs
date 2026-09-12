using UnityEngine;

/// <summary>
/// Enemy seeks the player at full speed when inside seek range.
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

        if (Vector2.Distance(_rb.position, player.position) > seekRange)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - _rb.position;
        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        Vector2 desiredVelocity = toPlayer.normalized * maxSpeed;
        Vector2 steering = Vector2.ClampMagnitude(
            desiredVelocity - _rb.linearVelocity,
            maxForce);

        Vector2 velocity = _rb.linearVelocity + steering * Time.fixedDeltaTime;
        _rb.linearVelocity = Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    void UpdateRangeCircle()
    {
        if (seekRangeCircle == null)
            return;

        SpriteRenderer sprite = seekRangeCircle.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return;

        float diameter = sprite.sprite.bounds.size.x;
        if (diameter <= 0f)
            return;

        seekRangeCircle.localScale = Vector3.one * (seekRange * 2f / diameter);
    }
}
