using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds a path to the target on the PlatformerNavGrid and follows it link by link.
/// Layers pick the waypoint set: Ground for land agents, Air for flyers, both for hybrids.
/// Ground links walk along x and jump when the link goes up or crosses a gap; air links fly straight to the node.
/// Replans every repathInterval seconds, but never in the middle of a jump.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlatformerPathFollower : MonoBehaviour
{
    [SerializeField] PlatformerNavGrid grid;
    [SerializeField] Transform target;
    [SerializeField] NavLayers layers = NavLayers.Ground;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField, Tooltip("How far above the landing surface a jump peaks, in world units.")]
    float jumpClearance = 1f;
    [SerializeField] float repathInterval = 0.5f;

    const float AlignTolerance = 0.05f;
    const float FootMargin = 0.05f;

    Rigidbody2D _rb;
    Collider2D _collider;
    float _gravityScale;

    List<NavLink> _path = new();
    int _index;          // next link to complete
    Vector2Int _from;    // node we are travelling away from
    float _nextRepath;
    bool _flying;        // gravity is off while flying
    bool _jumping;       // a jump has been launched and has not landed yet

    bool CanWalk => layers.HasFlag(NavLayers.Ground);
    bool CanFly => layers.HasFlag(NavLayers.Air);

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _gravityScale = _rb.gravityScale;

        SetFlying(!CanWalk); // flying-only agents never need gravity
    }

    void FixedUpdate()
    {
        if (!grid.IsReady || target == null)
            return;

        bool grounded = IsGrounded();
        bool landed = _jumping && grounded && _rb.linearVelocity.y <= 0f;

        // Replan while flying or standing on the ground, never mid-jump.
        if ((_flying || (grounded && !_jumping)) && Time.time >= _nextRepath)
            Repath(grounded);

        if (_index >= _path.Count)
            Idle();
        else if (_path[_index].Fly)
            FlyTo(_path[_index].To);
        else
            WalkTo(_path[_index].To, grounded, landed);
    }

    // ---------------------------------------------------------------------
    // Air links
    // ---------------------------------------------------------------------

    void FlyTo(Vector2Int next)
    {
        SetFlying(true);

        Vector2 center = _collider.bounds.center;
        Vector2 goal = grid.CellCenter(next);

        if (Vector2.Distance(center, goal) <= grid.CellSize * 0.25f)
            Advance(next);
        else
            _rb.linearVelocity = Vector2.ClampMagnitude((goal - center) / Time.fixedDeltaTime, moveSpeed);
    }

    // ---------------------------------------------------------------------
    // Ground links
    // ---------------------------------------------------------------------

    void WalkTo(Vector2Int next, bool grounded, bool landed)
    {
        SetFlying(false);

        Vector2 feet = Feet();

        // Arrived: standing (or just landed) inside the next node's cell.
        if ((landed || (grounded && !_jumping)) && grid.WorldToCell(feet) == next)
        {
            Advance(next);
            return;
        }

        if (landed)
            _jumping = false; // landed somewhere else; the next replan sorts it out

        if (PlatformerPathfinder.IsJump(_from, next))
            Jump(next, feet, grounded);
        else
            SetVelocityX(Steer(grid.CellCenter(next).x - feet.x));
    }

    void Jump(Vector2Int next, Vector2 feet, bool grounded)
    {
        Vector2 goal = grid.CellCenter(next);

        if (_jumping)
        {
            // Going up: rise straight until we are above the landing surface, then cross.
            // Level or downward jumps cross immediately.
            float landingSurfaceY = goal.y - grid.CellSize * 0.5f;
            bool mustRiseFirst = next.y > _from.y && feet.y < landingSurfaceY + 0.1f;

            SetVelocityX(mustRiseFirst ? 0f : Steer(goal.x - feet.x));
            return;
        }

        // Line up with the takeoff cell, then launch.
        float takeoffDx = grid.CellCenter(_from).x - feet.x;

        if (grounded && Mathf.Abs(takeoffDx) <= AlignTolerance)
            Launch(next);
        else
            SetVelocityX(Steer(takeoffDx));
    }

    void Launch(Vector2Int next)
    {
        // Kinematics: reaching height h needs v = sqrt(2 * g * h).
        float gravity = Mathf.Abs(Physics2D.gravity.y * _gravityScale);
        float height = Mathf.Max(next.y - _from.y, 0) * grid.CellSize + jumpClearance;

        _rb.linearVelocity = new Vector2(0f, Mathf.Sqrt(2f * gravity * height));
        _jumping = true;
    }

    // ---------------------------------------------------------------------
    // Planning
    // ---------------------------------------------------------------------

    void Repath(bool grounded)
    {
        _nextRepath = Time.time + repathInterval;
        _index = 0;
        _path.Clear();

        // Start: the standable cell under our feet when on the ground, otherwise the free cell around us.
        bool hasStart = CanWalk && grounded
            ? grid.TryFindStandable(Feet(), out Vector2Int start)
            : grid.TryFindFree(_collider.bounds.center, out start);

        // Goal: land agents want the floor under the target, flyers the target's own cell.
        // Hybrids try the floor first and fall back to the air.
        Vector2Int goal = default;
        bool hasGoal = CanWalk && grid.TryFindStandable(target.position, out goal);
        if (!hasGoal)
            hasGoal = CanFly && grid.TryFindFree(target.position, out goal);

        if (!hasStart || !hasGoal)
            return;

        _from = start;
        _path = PlatformerPathfinder.FindPath(grid, start, goal, layers) ?? _path; // no path: stand still
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    void Advance(Vector2Int reached)
    {
        _from = reached;
        _index++;
        _jumping = false;
    }

    void Idle()
    {
        if (_flying)
            _rb.linearVelocity = Vector2.zero; // hover where we are
        else
            SetVelocityX(0f);
    }

    void SetFlying(bool flying)
    {
        _flying = flying;
        _rb.gravityScale = flying ? 0f : _gravityScale;
    }

    // Speed that reaches dx this physics step, capped at moveSpeed (so we never overshoot).
    float Steer(float dx) => Mathf.Clamp(dx / Time.fixedDeltaTime, -moveSpeed, moveSpeed);

    void SetVelocityX(float x) => _rb.linearVelocity = new Vector2(x, _rb.linearVelocity.y);

    // Bottom centre of the collider, nudged up so it sits inside the cell we are standing in.
    Vector2 Feet()
    {
        Bounds b = _collider.bounds;
        return new Vector2(b.center.x, b.min.y + FootMargin);
    }

    bool IsGrounded()
    {
        Bounds b = _collider.bounds;
        Vector2 center = new(b.center.x, b.min.y - FootMargin * 0.5f);
        Vector2 size = new(b.size.x * 0.9f, FootMargin);
        return Physics2D.OverlapBox(center, size, 0f, grid.SolidLayers) != null;
    }

    void OnDrawGizmosSelected()
    {
        if (grid == null || !grid.IsReady)
            return;

        Vector3 previous = _collider != null ? _collider.bounds.center : transform.position;

        // Flights are blue, ground moves are magenta.
        for (int i = _index; i < _path.Count; i++)
        {
            Vector3 point = grid.CellCenter(_path[i].To);

            Gizmos.color = _path[i].Fly ? new Color(0.4f, 0.6f, 1f) : Color.magenta;
            Gizmos.DrawLine(previous, point);
            Gizmos.DrawWireSphere(point, 0.08f);
            previous = point;
        }
    }
}
