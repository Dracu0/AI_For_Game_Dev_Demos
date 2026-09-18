using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>Everything the demo needs after a search finishes.</summary>
    public struct SearchResult
    {
        public bool Found;
        public int Cost;
        public int NodesExpanded;
        public List<Vector2Int> Path;
        public List<Vector2Int> ExpansionOrder;

        public static SearchResult Failed()
        {
            return new SearchResult
            {
                Found = false,
                Cost = 0,
                NodesExpanded = 0,
                Path = new List<Vector2Int>(),
                ExpansionOrder = new List<Vector2Int>()
            };
        }
    }

    /// <summary>
    /// Teaching-friendly pathfinding.
    /// Dijkstra priority = g(n). A* priority = g(n) + h(n).
    /// Everything else is shared.
    /// </summary>
    public static class PathSolver
    {
        const int Unreachable = int.MaxValue / 4;
        const int CardinalStepCost = 1;
        const int DiagonalStepCost = 2;

        public static SearchResult RunDijkstra(Grid2D grid, Vector2Int start, Vector2Int goal) =>
            RunSearch(grid, start, goal, useHeuristic: false);

        public static SearchResult RunAStar(Grid2D grid, Vector2Int start, Vector2Int goal) =>
            RunSearch(grid, start, goal, useHeuristic: true);

        static SearchResult RunSearch(Grid2D grid, Vector2Int start, Vector2Int goal, bool useHeuristic)
        {
            if (!grid.IsWalkable(start.x, start.y) || !grid.IsWalkable(goal.x, goal.y))
                return SearchResult.Failed();

            int[,] costSoFar = CreateCostTable(grid, Unreachable);
            costSoFar[start.x, start.y] = 0;

            Vector2Int[,] cameFrom = new Vector2Int[grid.Width, grid.Height];
            MarkUnset(cameFrom);

            var visited = new HashSet<Vector2Int>();
            var openSet = new PriorityQueue<Vector2Int>();
            var expansionOrder = new List<Vector2Int>();

            openSet.Enqueue(start, Priority(start, goal, costSoFar, useHeuristic));

            while (!openSet.IsEmpty)
            {
                Vector2Int current = openSet.Dequeue();

                if (visited.Contains(current))
                    continue;

                visited.Add(current);
                expansionOrder.Add(current);

                if (current == goal)
                    return BuildSuccess(goal, costSoFar, cameFrom, expansionOrder);

                foreach (Vector2Int neighbour in grid.GetWalkableNeighbours(current))
                {
                    if (visited.Contains(neighbour))
                        continue;

                    int newCost = costSoFar[current.x, current.y] + GetStepCost(current, neighbour);

                    if (newCost >= costSoFar[neighbour.x, neighbour.y])
                        continue;

                    costSoFar[neighbour.x, neighbour.y] = newCost;
                    cameFrom[neighbour.x, neighbour.y] = current;
                    openSet.Enqueue(neighbour, Priority(neighbour, goal, costSoFar, useHeuristic));
                }
            }

            return BuildFailure(expansionOrder);
        }

        static float Priority(Vector2Int cell, Vector2Int goal, int[,] costSoFar, bool useHeuristic)
        {
            int g = costSoFar[cell.x, cell.y];
            return useHeuristic ? g + OctileDistance(cell, goal) : g;
        }

        static int GetStepCost(Vector2Int from, Vector2Int to)
        {
            int dx = Mathf.Abs(to.x - from.x);
            int dy = Mathf.Abs(to.y - from.y);
            return dx != 0 && dy != 0 ? DiagonalStepCost : CardinalStepCost;
        }

        static int OctileDistance(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            int min = Mathf.Min(dx, dy);
            int max = Mathf.Max(dx, dy);
            return min * DiagonalStepCost + (max - min) * CardinalStepCost;
        }

        static SearchResult BuildSuccess(
            Vector2Int goal,
            int[,] costSoFar,
            Vector2Int[,] cameFrom,
            List<Vector2Int> expansionOrder)
        {
            return new SearchResult
            {
                Found = true,
                Cost = costSoFar[goal.x, goal.y],
                NodesExpanded = expansionOrder.Count,
                Path = ReconstructPath(goal, cameFrom),
                ExpansionOrder = expansionOrder
            };
        }

        static SearchResult BuildFailure(List<Vector2Int> expansionOrder)
        {
            SearchResult failed = SearchResult.Failed();
            failed.ExpansionOrder = expansionOrder;
            failed.NodesExpanded = expansionOrder.Count;
            return failed;
        }

        static List<Vector2Int> ReconstructPath(Vector2Int goal, Vector2Int[,] cameFrom)
        {
            var path = new List<Vector2Int>();
            Vector2Int current = goal;

            while (true)
            {
                path.Add(current);

                Vector2Int previous = cameFrom[current.x, current.y];
                if (!IsSet(previous))
                    break;

                current = previous;
            }

            path.Reverse();
            return path;
        }

        static int[,] CreateCostTable(Grid2D grid, int defaultValue)
        {
            int[,] costs = new int[grid.Width, grid.Height];

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                    costs[x, y] = defaultValue;
            }

            return costs;
        }

        static void MarkUnset(Vector2Int[,] table)
        {
            for (int y = 0; y < table.GetLength(1); y++)
            {
                for (int x = 0; x < table.GetLength(0); x++)
                    table[x, y] = new Vector2Int(-1, -1);
            }
        }

        static bool IsSet(Vector2Int value) => value.x >= 0 && value.y >= 0;
    }
}
