using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class NodeNavigator : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;

    private Rigidbody2D _rb;
    private Camera _cam;
    private readonly List<GraphNode> _path = new();
    private readonly List<GraphNode> _candidate = new();
    private int _next;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            GoTo(_cam.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
    }

    public void GoTo(Vector2 target)
    {
        var goal = GraphNode.Nearest(target);
        if (goal == null) return;

        // Mid-edge: we're between the node we left and the node we're heading to, so try both.
        bool moving = _next < _path.Count;
        var ahead = moving ? _path[_next] : GraphNode.Nearest(_rb.position);
        var behind = moving && _next > 0 ? _path[_next - 1] : null;

        float best = float.MaxValue;
        TryStart(ahead, goal, ref best);
        TryStart(behind, goal, ref best);
    }

    // Keeps the route only if it exists and beats the best so far (agent -> first node included).
    private void TryStart(GraphNode start, GraphNode goal, ref float best)
    {
        if (start == null || !GraphNode.FindPath(start, goal, _candidate)) return;

        float length = Vector2.Distance(_rb.position, _candidate[0].Pos);
        for (int i = 1; i < _candidate.Count; i++)
            length += Vector2.Distance(_candidate[i - 1].Pos, _candidate[i].Pos);
        if (length >= best) return;

        best = length;
        _path.Clear();
        _path.AddRange(_candidate);
        _next = 0;
    }

    private void FixedUpdate()
    {
        // A waypoint counts as reached once within one physics step, so fast agents can't orbit it.
        float step = moveSpeed * Time.fixedDeltaTime;
        Vector2 to = default;

        while (_next < _path.Count)
        {
            to = _path[_next].Pos - _rb.position;
            if (to.sqrMagnitude > step * step) break;
            _next++;
        }

        _rb.linearVelocity = _next < _path.Count ? to.normalized * moveSpeed : Vector2.zero;
    }
}
