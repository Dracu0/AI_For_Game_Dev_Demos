using UnityEngine;

/// <summary>
/// Flee — run away from the player at full speed.
///
/// Goal: escape the player when they get too close.
/// Formula: desiredVelocity = directionAwayFromPlayer * maxSpeed
///
/// Only active inside flee range. Same steering math as seek, but the
/// direction is flipped.
///
/// Use when: cowardly NPC, scared animal, enemy retreating.
///
/// vs Seek: flee moves away from the player, not toward them.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FleeEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform player;

    [Header("Flee Range")]
    [SerializeField] Transform fleeRangeCircle;
    [SerializeField] float fleeRange = 5f;

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

        if (Vector2.Distance(_rb.position, player.position) > fleeRange)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 awayFromPlayer = _rb.position - (Vector2)player.position;
        if (awayFromPlayer.sqrMagnitude < 0.0001f)
            return;

        Vector2 desiredVelocity = awayFromPlayer.normalized * maxSpeed;
        Vector2 steering = Vector2.ClampMagnitude(
            desiredVelocity - _rb.linearVelocity,
            maxForce);

        Vector2 velocity = _rb.linearVelocity + steering * Time.fixedDeltaTime;
        _rb.linearVelocity = Vector2.ClampMagnitude(velocity, maxSpeed);
    }

    void UpdateRangeCircle()
    {
        if (fleeRangeCircle == null)
            return;

        SpriteRenderer sprite = fleeRangeCircle.GetComponent<SpriteRenderer>();
        if (sprite == null || sprite.sprite == null)
            return;

        float diameter = sprite.sprite.bounds.size.x;
        if (diameter <= 0f)
            return;

        fleeRangeCircle.localScale = Vector3.one * (fleeRange * 2f / diameter);
    }
}
