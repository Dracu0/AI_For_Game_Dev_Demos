using UnityEngine;

public class Evade : MonoBehaviour
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

        if (distance > detectionRadius)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 predicted = PredictPosition(
            target,
            position,
            _playerRb.linearVelocity
        );

        // Flee from where the player is about to be, not from where it is now: that is what makes Evade differ from Flee.
        Vector2 fleeDirection = position - predicted;
        if (fleeDirection.sqrMagnitude < 0.0001f) fleeDirection = position - target;   // prediction lands on us: fall back to plain Flee

        _rb.linearVelocity = fleeDirection.normalized * moveSpeed;
    }

    private Vector2 PredictPosition(
        Vector2 target,
        Vector2 current,
        Vector2 targetVelocity)
    {
        float predictionTime = Mathf.Min(
            Vector2.Distance(current, target) / Mathf.Max(moveSpeed, 0.01f),
            maxPredictionTime
        );

        return target + targetVelocity * predictionTime;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}