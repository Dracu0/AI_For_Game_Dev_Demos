using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

/// <summary>One move from a cell: where it ends, whether it is a flight, and what it costs.</summary>
public struct NavLink
{
    public Vector2Int To;
    public bool Fly;
    public float Cost;

    public NavLink(Vector2Int to, bool fly, float cost)
    {
        To = to;
        Fly = fly;
        Cost = cost;
    }
}

/// <summary>
/// A* over a PlatformerNavGrid. The layers decide which links an agent may use:
///   Ground: walk to a neighbour, jump up or across a gap, or step off a ledge and fall.
///   Air:    fly to any of the 8 neighbouring free cells.
/// </summary>
public static class PlatformerPathfinder
{
    const float JumpPenalty = 2f;   // makes the AI prefer walking over jumping when both reach the same place
    const float DiagonalCost = 1.41f;

    /// <summary>A ground move that goes up or crosses more than one cell needs a jump.</summary>
    public static bool IsJump(Vector2Int from, Vector2Int to) =>
        to.y > from.y || Mathf.Abs(to.x - from.x) > 1;

    /// <summary>Returns the links to follow in order (start excluded), or null when the goal is unreachable.</summary>
    public static List<NavLink> FindPath(PlatformerNavGrid grid, Vector2Int start, Vector2Int goal, NavLayers layers)
    {
        var cameFrom = new Dictionary<Vector2Int, (Vector2Int From, NavLink Link)>();
        var costSoFar = new Dictionary<Vector2Int, float> { [start] = 0f };
        var closed = new HashSet<Vector2Int>();
        var open = new PriorityQueue<Vector2Int>();
        var links = new List<NavLink>();

        open.Enqueue(start, 0f);

        while (!open.IsEmpty)
        {
            Vector2Int current = open.Dequeue();

            if (!closed.Add(current))
                continue;

            if (current == goal)
                return BuildPath(cameFrom, start, goal);

            GetLinks(grid, current, layers, links);
            foreach (NavLink link in links)
            {
                float newCost = costSoFar[current] + link.Cost;

                if (costSoFar.TryGetValue(link.To, out float oldCost) && newCost >= oldCost)
                    continue;

                costSoFar[link.To] = newCost;
                cameFrom[link.To] = (current, link);

                // Straight-line distance never exceeds the real cost, so A* stays optimal.
                open.Enqueue(link.To, newCost + Vector2Int.Distance(link.To, goal));
            }
        }

        return null;
    }

    /// <summary>Fills results with every move the given layers allow from this cell.</summary>
    public static void GetLinks(PlatformerNavGrid grid, Vector2Int from, NavLayers layers, List<NavLink> results)
    {
        results.Clear();

        if (layers.HasFlag(NavLayers.Ground) && grid.IsStandable(from))
            AddGroundLinks(grid, from, results);

        if (layers.HasFlag(NavLayers.Air) && grid.IsAirFree(from))
            AddAirLinks(grid, from, results);
    }

    static void AddGroundLinks(PlatformerNavGrid grid, Vector2Int from, List<NavLink> results)
    {
        for (int dx = -grid.MaxJumpWidth; dx <= grid.MaxJumpWidth; dx++)
        {
            for (int dy = -grid.MaxFallHeight; dy <= grid.MaxJumpHeight; dy++)
            {
                Vector2Int to = new(from.x + dx, from.y + dy);

                if (dx == 0 || !grid.IsStandable(to) || !PathIsClear(grid, from, to))
                    continue;

                float cost = Mathf.Abs(dx) + Mathf.Abs(dy) + (IsJump(from, to) ? JumpPenalty : 0f);
                results.Add(new NavLink(to, false, cost));
            }
        }
    }

    static void AddAirLinks(PlatformerNavGrid grid, Vector2Int from, List<NavLink> results)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector2Int to = new(from.x + dx, from.y + dy);
                bool diagonal = dx != 0 && dy != 0;

                if ((dx == 0 && dy == 0) || !grid.IsAirFree(to))
                    continue;

                // A diagonal needs both side cells free, otherwise it would cut through a wall corner.
                if (diagonal && !(grid.IsAirFree(new Vector2Int(to.x, from.y)) && grid.IsAirFree(new Vector2Int(from.x, to.y))))
                    continue;

                results.Add(new NavLink(to, true, (diagonal ? DiagonalCost : 1f) * grid.AirCostMultiplier));
            }
        }
    }

    // Moving up or level: rise first, then cross. Moving down: cross first, then fall.
    // Both are two straight lines, so "is there room?" is two simple line checks.
    static bool PathIsClear(PlatformerNavGrid grid, Vector2Int from, Vector2Int to)
    {
        Vector2Int corner = to.y >= from.y ? new Vector2Int(from.x, to.y) : new Vector2Int(to.x, from.y);
        return LineIsFree(grid, from, corner) && LineIsFree(grid, corner, to);
    }

    // a and b share a row or a column, so stepping by -1, 0 or +1 reaches b exactly.
    static bool LineIsFree(PlatformerNavGrid grid, Vector2Int a, Vector2Int b)
    {
        Vector2Int step = new(Mathf.Clamp(b.x - a.x, -1, 1), Mathf.Clamp(b.y - a.y, -1, 1));

        for (Vector2Int cell = a; ; cell += step)
        {
            if (!grid.IsFree(cell))
                return false;

            if (cell == b)
                return true;
        }
    }

    static List<NavLink> BuildPath(Dictionary<Vector2Int, (Vector2Int From, NavLink Link)> cameFrom, Vector2Int start, Vector2Int goal)
    {
        var path = new List<NavLink>();

        for (Vector2Int cell = goal; cell != start; cell = cameFrom[cell].From)
            path.Add(cameFrom[cell].Link);

        path.Reverse();
        return path;
    }
}
