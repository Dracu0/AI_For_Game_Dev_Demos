using System.Collections.Generic;
using UnityEngine;

public class GraphNode : MonoBehaviour
{
    public List<GraphNode> neighbors = new();

    public Vector2 Pos { get; private set; }   // cached: nodes are assumed not to move

    private static readonly List<GraphNode> All = new();
    private static readonly Dictionary<GraphNode, float> Cost = new();
    private static readonly Dictionary<GraphNode, GraphNode> From = new();
    private static readonly List<GraphNode> Open = new();

    private void OnEnable()
    {
        Pos = transform.position;
        All.Add(this);

        neighbors.RemoveAll(n => n == null);
        foreach (var n in neighbors)
            if (!n.neighbors.Contains(this)) n.neighbors.Add(this);   // links are always two-way
    }

    private void OnDisable() => All.Remove(this);

    public static GraphNode Nearest(Vector2 p)
    {
        GraphNode best = null;
        float bestSqr = float.MaxValue;
        foreach (var n in All)
        {
            float sqr = (n.Pos - p).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = n; }
        }
        return best;
    }

    public static bool FindPath(GraphNode start, GraphNode goal, List<GraphNode> path)
    {
        path.Clear(); Cost.Clear(); From.Clear(); Open.Clear();
        Cost[start] = 0; Open.Add(start);
        Vector2 goalPos = goal.Pos;

        while (Open.Count > 0)
        {
            int bestIndex = 0;
            float bestF = float.MaxValue;
            for (int i = 0; i < Open.Count; i++)
            {
                float f = Cost[Open[i]] + Vector2.Distance(Open[i].Pos, goalPos);
                if (f < bestF) { bestF = f; bestIndex = i; }
            }

            var cur = Open[bestIndex];
            int last = Open.Count - 1;
            Open[bestIndex] = Open[last];   // swap-remove: order doesn't matter, so O(1)
            Open.RemoveAt(last);

            if (cur == goal)
            {
                for (var n = goal; n != null; n = From.GetValueOrDefault(n)) path.Add(n);
                path.Reverse();
                return true;
            }

            float g = Cost[cur];
            foreach (var nb in cur.neighbors)
            {
                if (!nb.isActiveAndEnabled) continue;   // disabled nodes act as blocked

                float c = g + Vector2.Distance(cur.Pos, nb.Pos);
                if (Cost.TryGetValue(nb, out float old) && c >= old) continue;

                Cost[nb] = c; From[nb] = cur;
                if (!Open.Contains(nb)) Open.Add(nb);
            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        foreach (var n in neighbors)
            if (n != null) Gizmos.DrawLine(transform.position, n.transform.position);
    }
}
