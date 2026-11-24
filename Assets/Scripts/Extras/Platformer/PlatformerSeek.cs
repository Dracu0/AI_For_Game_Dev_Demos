using UnityEngine;

/// <summary>
/// Simplest platformer seek: walk toward the target's x position and (optionally) stop at ledges.
/// Set groundLayers to the ground layers only. Gravity comes from the Rigidbody2D.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlatformerSeek : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float stopDistance = 0.5f;
    [SerializeField] bool detectLedges = true;

    [Header("Sensing")]
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] float probeSize = 0.3f;
    [SerializeField] float stepHeight = 0.15f;

    const float FootMargin = 0.05f;

    Rigidbody2D _rb;
    Collider2D _collider;

    public Transform Target => target;

    float DeltaX => target.position.x - transform.position.x;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        // Seek: only the x axis matters in a platformer. Sign gives -1 or +1; inside stopDistance we stand still.
        float dir = Mathf.Abs(DeltaX) > stopDistance ? Mathf.Sign(DeltaX) : 0f;

        // Stop at a ledge instead of walking off it.
        if (detectLedges && LedgeAhead(dir))
            dir = 0f;

        Vector2 velocity = _rb.linearVelocity;
        velocity.x = dir * moveSpeed;
        _rb.linearVelocity = velocity;
    }

    /// <summary>True when standing on ground with nothing to stand on just ahead (dir is -1 or +1).</summary>
    public bool LedgeAhead(float dir)
    {
        Bounds b = _collider.bounds;
        return Hit(GroundProbe(b)) && !Hit(LedgeProbe(b, dir));
    }

    /// <summary>True when a wall blocks the way just ahead (dir is -1 or +1).</summary>
    public bool WallAhead(float dir) => Hit(WallProbe(_collider.bounds, dir));

    bool Hit(Rect probe) => Physics2D.OverlapBox(probe.center, probe.size, 0f, groundLayers) != null;

    // All probes are built from the collider bounds b: b.center, b.extents (half size), b.size and b.min.y (the feet).
    // dir is -1 (left) or +1 (right), so "b.center.x + dir * b.extents.x" is the body's side edge.

    // Thin strip just under the feet, slightly narrower than the body so touching a wall does not count as ground.
    // Spans y = min.y down to min.y - FootMargin, so its center is half a margin below the feet.
    Rect GroundProbe(Bounds b) =>
        Box(b.center.x, b.min.y - FootMargin * 0.5f, b.size.x * 0.9f, FootMargin);

    // Box ahead of the body, half the body's height. Its bottom edge is stepHeight above the feet so the floor
    // and small bumps are ignored. Center y = bottom + half the box height = min.y + stepHeight + size.y * 0.25.
    Rect WallProbe(Bounds b, float dir) =>
        Box(b.center.x + dir * (b.extents.x + probeSize * 0.5f), b.min.y + stepHeight + b.size.y * 0.25f, probeSize, b.size.y * 0.5f);

    // Box below the feet, just past the body edge. Its top edge sits FootMargin above the feet so it overlaps the
    // floor surface when ground is there; an empty box means a drop. Center y = top - half the box height.
    Rect LedgeProbe(Bounds b, float dir) =>
        Box(b.center.x + dir * (b.extents.x + probeSize * 0.5f), b.min.y + FootMargin - probeSize * 0.5f, probeSize, probeSize);

    // Rect wants its bottom-left corner, but the probes above are described by their center, so shift by half the size.
    static Rect Box(float x, float y, float width, float height) =>
        new(x - width * 0.5f, y - height * 0.5f, width, height);

    void OnDrawGizmosSelected()
    {
        Bounds b = GetComponent<Collider2D>().bounds;
        float dir = 1f;

        if (target != null)
        {
            dir = Mathf.Sign(DeltaX);
            Gizmos.color = Mathf.Abs(DeltaX) > stopDistance ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, new Vector3(target.position.x, transform.position.y));
        }

        bool play = Application.isPlaying;
        DrawProbe(GroundProbe(b), play && Hit(GroundProbe(b)), Color.green, Color.red);

        DrawProbe(WallProbe(b, dir), play && Hit(WallProbe(b, dir)), Color.red, Color.yellow);

        if (detectLedges)
            DrawProbe(LedgeProbe(b, dir), play && !Hit(LedgeProbe(b, dir)), Color.magenta, Color.cyan);
    }

    static void DrawProbe(Rect probe, bool active, Color activeColor, Color idleColor)
    {
        Gizmos.color = active ? activeColor : idleColor;
        Gizmos.DrawWireCube(probe.center, probe.size);
    }
}
