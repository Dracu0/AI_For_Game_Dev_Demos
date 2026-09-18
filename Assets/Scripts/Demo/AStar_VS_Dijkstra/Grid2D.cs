using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Walkability grid for pathfinding demos. (0,0) is bottom-left; moves are 8-way with costs 1 / 2.
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

            TryAddNeighbour(neighbours, cell.x + 1, cell.y);
            TryAddNeighbour(neighbours, cell.x - 1, cell.y);
            TryAddNeighbour(neighbours, cell.x, cell.y + 1);
            TryAddNeighbour(neighbours, cell.x, cell.y - 1);

            TryAddDiagonalNeighbour(neighbours, cell, cell.x + 1, cell.y + 1);
            TryAddDiagonalNeighbour(neighbours, cell, cell.x + 1, cell.y - 1);
            TryAddDiagonalNeighbour(neighbours, cell, cell.x - 1, cell.y + 1);
            TryAddDiagonalNeighbour(neighbours, cell, cell.x - 1, cell.y - 1);

            return neighbours;
        }

        void TryAddNeighbour(List<Vector2Int> neighbours, int x, int y)
        {
            if (IsWalkable(x, y))
                neighbours.Add(new Vector2Int(x, y));
        }

        void TryAddDiagonalNeighbour(List<Vector2Int> neighbours, Vector2Int from, int x, int y)
        {
            if (!IsWalkable(x, y))
                return;

            bool cardinalX = IsWalkable(from.x, y);
            bool cardinalY = IsWalkable(x, from.y);

            if (cardinalX != cardinalY)
                return;

            neighbours.Add(new Vector2Int(x, y));
        }
    }
}
