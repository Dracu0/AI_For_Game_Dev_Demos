using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Walkability grid. (0,0) is bottom-left.
    /// Movement is 8-way: cardinal cost 1, diagonal cost 2.
    /// </summary>
    public class Grid2D
    {
        public int Width { get; }
        public int Height { get; }

        readonly bool[] _walkable;

        public Grid2D(int width, int height)
        {
            Width = width;
            Height = height;
            _walkable = new bool[width * height];

            for (int i = 0; i < _walkable.Length; i++)
                _walkable[i] = true;
        }

        public bool InBounds(int x, int y) =>
            x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsWalkable(int x, int y) =>
            InBounds(x, y) && _walkable[y * Width + x];

        public void SetWalkable(int x, int y, bool walkable)
        {
            if (InBounds(x, y))
                _walkable[y * Width + x] = walkable;
        }

        public List<Vector2Int> GetWalkableNeighbours(Vector2Int cell)
        {
            var neighbours = new List<Vector2Int>(8);

            TryAdd(neighbours, cell.x + 1, cell.y);
            TryAdd(neighbours, cell.x - 1, cell.y);
            TryAdd(neighbours, cell.x, cell.y + 1);
            TryAdd(neighbours, cell.x, cell.y - 1);

            TryAddDiagonal(neighbours, cell, cell.x + 1, cell.y + 1);
            TryAddDiagonal(neighbours, cell, cell.x + 1, cell.y - 1);
            TryAddDiagonal(neighbours, cell, cell.x - 1, cell.y + 1);
            TryAddDiagonal(neighbours, cell, cell.x - 1, cell.y - 1);

            return neighbours;
        }

        void TryAdd(List<Vector2Int> neighbours, int x, int y)
        {
            if (IsWalkable(x, y))
                neighbours.Add(new Vector2Int(x, y));
        }

        void TryAddDiagonal(List<Vector2Int> neighbours, Vector2Int from, int x, int y)
        {
            if (!IsWalkable(x, y))
                return;

            // Both adjacent cardinal tiles must be free, otherwise the
            // diagonal would cut through a wall corner.
            bool sideOpen = IsWalkable(from.x, y);
            bool otherSideOpen = IsWalkable(x, from.y);
            if (!sideOpen || !otherSideOpen)
                return;

            neighbours.Add(new Vector2Int(x, y));
        }
    }
}
