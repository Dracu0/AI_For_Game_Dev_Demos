using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Pure grid data model used by the search algorithms.
    /// Independent of Unity rendering so it can be tested in isolation.
    /// Coordinate convention: index = y * Width + x, (0,0) is the bottom-left.
    /// Movement is 8-directional (cardinals + diagonals) with integer step costs (1 / 2).
    /// Diagonal steps are allowed through gaps between walls, but not through single-wall corners.
    /// </summary>
    public class Grid2D
    {
        // ------------------------------------------------------------------
        // Grid size
        // ------------------------------------------------------------------

        public int Width { get; }
        public int Height { get; }
        public int CellCount => Width * Height;

        // ------------------------------------------------------------------
        // Cell data
        // ------------------------------------------------------------------

        readonly bool[] _walkable;

        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        public Grid2D(int width, int height)
        {
            Width = width;
            Height = height;
            _walkable = new bool[width * height];

            for (int i = 0; i < _walkable.Length; i++)
                _walkable[i] = true;
        }

        // ------------------------------------------------------------------
        // Bounds and walkability
        // ------------------------------------------------------------------

        public bool InBounds(int x, int y) =>
            x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsWalkable(int x, int y) =>
            InBounds(x, y) && _walkable[y * Width + x];

        public void SetWalkable(int x, int y, bool walkable)
        {
            if (InBounds(x, y))
                _walkable[y * Width + x] = walkable;
        }

        // ------------------------------------------------------------------
        // Coordinates
        // ------------------------------------------------------------------

        public int IndexOf(int x, int y) => y * Width + x;

        public void GetCoords(int index, out int x, out int y)
        {
            x = index % Width;
            y = index / Width;
        }

        // ------------------------------------------------------------------
        // Neighbours (8-directional movement)
        // ------------------------------------------------------------------

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

            // Allow diagonals through gaps where both cardinals are walls.
            // Block only when exactly one cardinal is blocked (corner-cutting).
            if (cardinalX != cardinalY)
                return;

            neighbours.Add(new Vector2Int(x, y));
        }
    }
}
