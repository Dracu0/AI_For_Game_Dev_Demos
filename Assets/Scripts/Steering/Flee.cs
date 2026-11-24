using UnityEngine;

public class Flee : MonoBehaviour
{
    private Transform _playerTransform;
    private Rigidbody2D _rb;

    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private Color detectionColor = Color.red;

    private void Awake()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;

        _rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null) return;

        Vector2 direction = _rb.position - (Vector2)_playerTransform.position;

        float distance = direction.magnitude;

        _rb.linearVelocity = distance <= detectionRadius && distance > 0.1f
            ? direction.normalized * moveSpeed
            : Vector2.zero;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}