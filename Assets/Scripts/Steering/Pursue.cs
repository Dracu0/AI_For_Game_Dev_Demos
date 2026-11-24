using UnityEngine;

public class Pursue : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Rigidbody2D _playerRb;

    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float maxPredictionTime = 1f;
    [SerializeField] private Color detectionColor = Color.red;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerRb = player.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (_playerRb == null) return;

        Vector2 position = _rb.position;
        Vector2 target = _playerRb.position;
        float distance = Vector2.Distance(position, target);

        if (distance > detectionRadius || distance <= 0.1f)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 predicted = PredictPosition(
            target, position, _playerRb.linearVelocity, moveSpeed
        );

        _rb.linearVelocity = (predicted - position).normalized * moveSpeed;
    }

    private Vector2 PredictPosition(
        Vector2 target, Vector2 current, Vector2 velocity, float speed)
    {
        if (speed <= 0f) return target;

        float time = Mathf.Min(
            Vector2.Distance(current, target) / speed,
            maxPredictionTime
        );

        return target + velocity * time;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}