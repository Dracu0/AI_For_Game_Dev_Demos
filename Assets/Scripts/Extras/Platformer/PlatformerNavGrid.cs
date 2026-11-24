using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Which waypoint set an agent may use. Hybrids tick both.</summary>
[Flags]
public enum NavLayers
{
    None = 0,
    Ground = 1, // standable cells: walk, jump and drop links
    Air = 2     // every free cell: 8-way flying links
}

/// <summary>
/// Turns a Tilemap's collider into a grid of nodes. Cells touching the collider are obstacles, the rest are free.
/// Two waypoint sets come out of it:
///   Ground: free cells with an obstacle directly below (where land agents stand).
///   Air:    every free cell, next to colliders or not (where flyers move).
/// Everything is classified once in Build(), so every query afterwards is a single array lookup.
/// </summary>
public class PlatformerNavGrid : MonoBehaviour
{
    [SerializeField] Tilemap tilemap;
    [SerializeField, Tooltip("Empty cells added around the tilemap so there is air above the top platforms.")]
    int padding = 8;

    [Header("Ground set")]
    [SerializeField, Tooltip("Cells a land agent needs free to stand in (collider height / cell size, rounded up).")]
    int groundAgentHeight = 2;
    [SerializeField] int maxJumpHeight = 4;
    [SerializeField] int maxJumpWidth = 4;
    [SerializeField] int maxFallHeight = 20;

    [Header("Air set")]
    [SerializeField, Tooltip("Cells a flying agent needs free around its centre.")]
    int airAgentHeight = 1;
    [SerializeField, Tooltip("Hybrids prefer walking while this is above 1. No effect on flying-only agents.")]
    float airCostMultiplier = 1.5f;

    [Header("Gizmos (select this object in play mode)")]
    [SerializeField] NavLayers showNodes = NavLayers.Ground;
    [SerializeField] NavLayers showLinks = NavLayers.None;

    [Flags]
    enum Cell : byte
    {
        Solid = 1,      // overlaps the tilemap collider
        GroundFree = 2, // room for a land agent
        AirFree = 4,    // room for a flying agent
        Standable = 8   // GroundFree with a solid cell below
    }

    const int SearchDepth = 12;
    static readonly Color AirColor = new(0.4f, 0.6f, 1f);

    Cell[,] _cells;
    Vector2Int _origin; // cell coordinates of _cells[0, 0]

    public bool IsReady { get; private set; }
    public LayerMask SolidLayers { get; private set; }
    public float CellSize => tilemap.cellSize.x;
    public int MaxJumpHeight => maxJumpHeight;
    public int MaxJumpWidth => maxJumpWidth;
    public int MaxFallHeight => maxFallHeight;
    public float AirCostMultiplier => airCostMultiplier;

    IEnumerator Start()
    {
        // Wait one physics step so the tilemap collider is registered before we query it.
        yield return new WaitForFixedUpdate();
        Build();
    }

    [ContextMenu("Rebuild")]
    public void Build()
    {
        Collider2D solid = tilemap.GetComponent<CompositeCollider2D>();
        if (solid == null)
            solid = tilemap.GetComponent<TilemapCollider2D>();

        SolidLayers = 1 << tilemap.gameObject.layer;

        BoundsInt bounds = tilemap.cellBounds;
        _origin = new Vector2Int(bounds.xMin - padding, bounds.yMin - padding);
        _cells = new Cell[bounds.size.x + padding * 2, bounds.size.y + padding * 2];

        MarkSolidCells(solid);
        ClassifyFreeCells();
        IsReady = true;
    }

    // A cell is solid when the tilemap collider overlaps (most of) it.
    void MarkSolidCells(Collider2D solid)
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = SolidLayers, useTriggers = true };
        var hits = new List<Collider2D>();
        Vector2 probeSize = tilemap.cellSize * 0.8f;

        ForEachCell(cell =>
        {
            Physics2D.OverlapBox(CellCenter(cell), probeSize, 0f, filter, hits);
            if (hits.Contains(solid))
                Set(cell, Cell.Solid);
        });
    }

    void ClassifyFreeCells() => ForEachCell(cell =>
    {
        if (IsBlocked(cell))
            return;

        if (HasRoomAbove(cell, groundAgentHeight))
        {
            Set(cell, Cell.GroundFree);

            if (IsBlocked(cell + Vector2Int.down))
                Set(cell, Cell.Standable);
        }

        if (HasRoomAbove(cell, airAgentHeight))
            Set(cell, Cell.AirFree);
    });

    bool HasRoomAbove(Vector2Int cell, int height)
    {
        for (int i = 0; i < height; i++)
        {
            if (IsBlocked(cell + new Vector2Int(0, i)))
                return false;
        }

        return true;
    }

    // ---------------------------------------------------------------------
    // Queries
    // ---------------------------------------------------------------------

    public Vector2Int WorldToCell(Vector2 world)
    {
        Vector3Int cell = tilemap.WorldToCell(world);
        return new Vector2Int(cell.x, cell.y);
    }

    public Vector2 CellCenter(Vector2Int cell) =>
        tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

    /// <summary>Outside the grid counts as an obstacle, so paths can never leave it.</summary>
    public bool IsBlocked(Vector2Int cell) => Has(cell, Cell.Solid, outside: true);

    /// <summary>Room for a land agent (its whole height is free).</summary>
    public bool IsFree(Vector2Int cell) => Has(cell, Cell.GroundFree);

    /// <summary>Room for a flying agent. Any free cell is an air node, whatever is around it.</summary>
    public bool IsAirFree(Vector2Int cell) => Has(cell, Cell.AirFree);

    /// <summary>Ground set: room to stand, with an obstacle directly below.</summary>
    public bool IsStandable(Vector2Int cell) => Has(cell, Cell.Standable);

    /// <summary>
    /// Ground set lookup: looks straight down for the floor (the position may be mid-jump),
    /// then tries the neighbouring columns.
    /// </summary>
    public bool TryFindStandable(Vector2 world, out Vector2Int result)
    {
        Vector2Int cell = WorldToCell(world);

        for (int i = 0; i < 3; i++)
        {
            int dx = i == 0 ? 0 : i == 1 ? -1 : 1;

            for (int dy = 0; dy <= SearchDepth; dy++)
            {
                Vector2Int candidate = new(cell.x + dx, cell.y - dy);

                if (IsBlocked(candidate))
                    break;

                if (IsStandable(candidate))
                {
                    result = candidate;
                    return true;
                }
            }
        }

        result = cell;
        return false;
    }

    /// <summary>Air set lookup: the cell at this position, or the closest free cell within two cells of it.</summary>
    public bool TryFindFree(Vector2 world, out Vector2Int result)
    {
        Vector2Int cell = WorldToCell(world);

        for (int radius = 0; radius <= 2; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int candidate = new(cell.x + dx, cell.y + dy);

                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) == radius && IsAirFree(candidate))
                    {
                        result = candidate;
                        return true;
                    }
                }
            }
        }

        result = cell;
        return false;
    }

    bool Has(Vector2Int cell, Cell flag, bool outside = false)
    {
        int x = cell.x - _origin.x;
        int y = cell.y - _origin.y;

        if (x < 0 || y < 0 || x >= _cells.GetLength(0) || y >= _cells.GetLength(1))
            return outside;

        return (_cells[x, y] & flag) != 0;
    }

    void Set(Vector2Int cell, Cell flag) => _cells[cell.x - _origin.x, cell.y - _origin.y] |= flag;

    void ForEachCell(Action<Vector2Int> action)
    {
        for (int x = 0; x < _cells.GetLength(0); x++)
        {
            for (int y = 0; y < _cells.GetLength(1); y++)
                action(new Vector2Int(_origin.x + x, _origin.y + y));
        }
    }

    // ---------------------------------------------------------------------
    // Gizmos
    // ---------------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        if (!IsReady)
            return;

        var links = new List<NavLink>();

        ForEachCell(cell =>
        {
            Vector2 center = CellCenter(cell);

            if (IsBlocked(cell))
                DrawCell(center, 0.9f, new Color(1f, 0.2f, 0.2f, 0.25f));

            if (showNodes.HasFlag(NavLayers.Air) && IsAirFree(cell))
                DrawCell(center, 0.15f, new Color(AirColor.r, AirColor.g, AirColor.b, 0.5f));

            if (showNodes.HasFlag(NavLayers.Ground) && IsStandable(cell))
                DrawCell(center, 0.3f, new Color(0.2f, 1f, 0.3f, 0.8f));

            if (showLinks != NavLayers.None)
                DrawLinks(cell, links);
        });
    }

    void DrawCell(Vector2 center, float scale, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawCube(center, Vector3.one * CellSize * scale);
    }

    // Flights are blue, jumps yellow, walks and drops cyan.
    void DrawLinks(Vector2Int from, List<NavLink> links)
    {
        PlatformerPathfinder.GetLinks(this, from, showLinks, links);

        foreach (NavLink link in links)
        {
            Gizmos.color = link.Fly ? AirColor : PlatformerPathfinder.IsJump(from, link.To) ? Color.yellow : Color.cyan;
            Gizmos.DrawLine(CellCenter(from), CellCenter(link.To));
        }
    }
}
