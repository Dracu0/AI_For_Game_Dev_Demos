using UnityEngine;

/// <summary>
/// Applies InputManager.Movement to this object's Rigidbody2D.
/// Reads input in Update, then moves in FixedUpdate.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;

    Rigidbody2D _rb;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    void FixedUpdate() =>
        _rb.linearVelocity = InputManager.Movement * moveSpeed;
}
