using UnityEngine;

public class Arrival : MonoBehaviour
{
    private Transform _playerTransform;
    private Rigidbody2D _rb;

    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float slowRadius = 2.5f;
    [SerializeField] private Color detectionColor = Color.red;
    [SerializeField] private Color slowColor = Color.blue;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;

        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null) return;

        Vector2 direction = (Vector2)_playerTransform.position - _rb.position;
        float distance = direction.magnitude;

        float speed = distance <= slowRadius
            ? moveSpeed * Mathf.InverseLerp(0.1f, slowRadius, distance)
            : distance <= detectionRadius ? moveSpeed : 0f;

        _rb.linearVelocity = distance > 0.1f
            ? direction.normalized * speed
            : Vector2.zero;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = slowColor;
        Gizmos.DrawWireSphere(transform.position, slowRadius);
    }
}