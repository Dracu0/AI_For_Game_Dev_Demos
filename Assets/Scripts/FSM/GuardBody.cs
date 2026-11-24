using System.Collections;
using UnityEngine;

// PROVIDED: not typed in class. Everything the guard can DO and SENSE, but no decisions.
// The decisions (the state machine) live in GuardAI, which students write.
//
// Rules for GuardAI (it inherits from this class):
//   - use Start / FixedUpdate freely;
//   - do not declare Awake, OnValidate, OnGUI or OnDrawGizmosSelected: this class already uses them.
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public abstract class GuardBody : MonoBehaviour
{
    [Header("Route")]
    public Transform[] patrolPoints;

    [Header("Speeds")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 3.5f;

    [Header("Senses")]
    public float sightRadius = 3f;      // start chasing inside this
    public float loseRadius = 5.5f;     // give up outside this (bigger on purpose: hysteresis)

    [Header("Used later in the lesson")]
    public float attackRange = 1f;
    public float attackCooldown = 1f;   // seconds between strikes
    public float searchTime = 3f;       // seconds spent looking around

    private const float ArriveDistance = 0.5f;
    private const float FlashTime = 0.15f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sprite;
    private Transform _player;
    private int _patrolIndex;
    private Color _color = Color.white;
    private string _label = "";
    private GUIStyle _labelStyle;

    // ---------- Senses ----------

    // No player in the scene = infinitely far away, so the guard simply never notices anyone.
    public float DistanceToPlayer =>
        _player == null ? float.PositiveInfinity : Vector2.Distance(_rb.position, _player.position);

    public Vector2 PlayerPosition => _player == null ? _rb.position : (Vector2)_player.position;

    // ---------- Movement ----------

    // Walks toward a point. Returns true once the guard is close enough to count as arrived.
    public bool MoveToward(Vector2 target, float speed)
    {
        Vector2 toTarget = target - _rb.position;
        if (toTarget.magnitude <= ArriveDistance)
        {
            _rb.linearVelocity = Vector2.zero;
            return true;
        }

        _rb.linearVelocity = toTarget.normalized * speed;
        return false;
    }

    public void Stop() => _rb.linearVelocity = Vector2.zero;

    // ---------- Patrol route ----------

    // The point the guard is currently heading for.
    public Vector2 PatrolPoint =>
        patrolPoints.Length == 0 ? _rb.position : (Vector2)patrolPoints[_patrolIndex].position;

    public void NextPatrolPoint()
    {
        if (patrolPoints.Length > 0)
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;
    }

    public void GoToNearestPatrolPoint()
    {
        float best = float.MaxValue;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector2.Distance(patrolPoints[i].position, _rb.position);
            if (distance < best) { best = distance; _patrolIndex = i; }
        }
    }

    // ---------- Showing the state ----------

    // Tint the guard and write the state's name above it, so the whole room can follow along.
    public void Show(string label, Color color)
    {
        _label = label;
        _color = color;
        _sprite.color = color;
    }

    // A quick white blink that returns to the current state's color.
    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        _sprite.color = Color.white;
        yield return new WaitForSeconds(FlashTime);
        _sprite.color = _color;
    }

    // ---------- Plumbing ----------

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _player = player.transform;

        if (patrolPoints == null || patrolPoints.Length == 0)
            Debug.LogWarning($"{name}: no patrol points assigned, the guard will have no route.", this);
    }

    // Keeps the numbers sane even if someone types nonsense in the Inspector.
    private void OnValidate()
    {
        sightRadius = Mathf.Max(0f, sightRadius);
        loseRadius = Mathf.Max(loseRadius, sightRadius);
        attackRange = Mathf.Clamp(attackRange, 0f, sightRadius);
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(_label) || Camera.main == null) return;

        _labelStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.7f);
        var rect = new Rect(screen.x - 80f, Screen.height - screen.y - 12f, 160f, 24f);

        _labelStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), _label, _labelStyle);
        _labelStyle.normal.textColor = Color.white;
        GUI.Label(rect, _label, _labelStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseRadius);
    }
}
