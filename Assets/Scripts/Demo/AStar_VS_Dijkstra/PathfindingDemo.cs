using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Simple A* vs Dijkstra demo using one tile prefab.
///
/// Controls:
///   LMB   - set start, then target
///   RMB   - toggle obstacle
///   Space - run A* → then Dijkstra → then show comparison stats
/// </summary>
public class PathfindingDemo : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Demo flow state
    // ------------------------------------------------------------------

    enum DemoPhase
    {
        Editing,
        WaitingForDijkstra,
        WaitingForStats,
        ShowingStats
    }

    // ------------------------------------------------------------------
    // Inspector settings
    // ------------------------------------------------------------------

    [Header("Scene")]
    [SerializeField] Grid grid;
    [SerializeField] GameObject tilePrefab;
    [SerializeField] int gridWidth = 15;
    [SerializeField] int gridHeight = 10;

    [Header("Colors")]
    [SerializeField] Color baseColor = Color.white;
    [SerializeField] Color obstacleColor = Color.gray;
    [SerializeField] Color startColor = Color.green;
    [SerializeField] Color targetColor = Color.red;
    [SerializeField] Color visitedColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] Color pathColor = Color.yellow;

    [Header("UI (optional)")]
    [SerializeField] TMP_Text statusText;

    [Header("Animation")]
    [SerializeField] float stepDelay = 0.03f;

    // ------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------

    readonly Dictionary<Vector2Int, SpriteRenderer> _cells = new Dictionary<Vector2Int, SpriteRenderer>();
    readonly HashSet<Vector2Int> _obstacles = new HashSet<Vector2Int>();

    Grid2D _gridData;
    Vector2Int _start = new Vector2Int(-1, -1);
    Vector2Int _target = new Vector2Int(-1, -1);
    SearchResult _astarResult;
    SearchResult _dijkstraResult;
    DemoPhase _phase = DemoPhase.Editing;

    Transform _cellsParent;
    Camera _camera;
    bool _animating;

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    void Awake()
    {
        _camera = Camera.main;

        if (!ValidateSetup())
            return;

        _cellsParent = new GameObject("Cells").transform;
        _cellsParent.SetParent(transform);

        BuildGrid();
        FrameCamera();
        SetStatus("LMB: start, then target. RMB: obstacles. Space: run A*.");
    }

    void Update()
    {
        if (_animating)
            return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            HandleSpacePressed();
            return;
        }

        if (!CanEditMap())
            return;

        if (IsPointerOverUI())
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            HandleLeftClick();

        if (Mouse.current.rightButton.wasPressedThisFrame)
            HandleRightClick();
    }

    // ------------------------------------------------------------------
    // Demo sequence (Space key)
    // ------------------------------------------------------------------

    void HandleSpacePressed()
    {
        switch (_phase)
        {
            case DemoPhase.Editing:
                if (_start.x < 0 || _target.x < 0)
                {
                    SetStatus("Set start and target with LMB first.");
                    return;
                }

                StartCoroutine(RunAStarPhase());
                break;

            case DemoPhase.WaitingForDijkstra:
                StartCoroutine(RunDijkstraPhase());
                break;

            case DemoPhase.WaitingForStats:
                ClearSearchVisuals();
                ShowComparisonStats();
                _phase = DemoPhase.ShowingStats;
                break;

            case DemoPhase.ShowingStats:
                _phase = DemoPhase.Editing;
                SetStatus("LMB: start, then target. RMB: obstacles. Space: run A*.");
                break;
        }
    }

    IEnumerator RunAStarPhase()
    {
        _animating = true;
        RebuildGridData();

        _astarResult = PathSolver.RunAStar(_gridData, _start, _target);

        ClearSearchVisuals();
        SetStatus("Running A*...");
        yield return AnimateSearch(_astarResult);

        _animating = false;
        _phase = DemoPhase.WaitingForDijkstra;
        SetStatus(_astarResult.Found
            ? "A* finished. Press Space to clear and run Dijkstra."
            : "A* found no path. Press Space to clear and run Dijkstra.");
    }

    IEnumerator RunDijkstraPhase()
    {
        _animating = true;
        RebuildGridData();

        ClearSearchVisuals();

        _dijkstraResult = PathSolver.RunDijkstra(_gridData, _start, _target);

        SetStatus("Running Dijkstra...");
        yield return AnimateSearch(_dijkstraResult);

        _animating = false;
        _phase = DemoPhase.WaitingForStats;
        SetStatus(_dijkstraResult.Found
            ? "Dijkstra finished. Press Space to clear."
            : "Dijkstra found no path. Press Space to clear.");
    }

    void ShowComparisonStats()
    {
        SetStatus(
            "Results\n" +
            $"{FormatResultLine("A*", _astarResult)}\n" +
            $"{FormatResultLine("Dijkstra", _dijkstraResult)}\n\n" +
            "Edit map or press Space to set up again.");
    }

    static string FormatResultLine(string name, SearchResult result)
    {
        if (result.Found)
            return $"{name}: {result.NodesExpanded} tiles searched | path cost {result.Cost}";

        return $"{name}: no path | {result.NodesExpanded} tiles searched";
    }

    // ------------------------------------------------------------------
    // Scene setup
    // ------------------------------------------------------------------

    bool ValidateSetup()
    {
        if (grid == null || tilePrefab == null)
        {
            Debug.LogError("PathfindingDemo: assign the Grid and tile prefab.");
            return false;
        }

        if (tilePrefab.GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogError("PathfindingDemo: the tile prefab needs a SpriteRenderer.");
            return false;
        }

        if (_camera == null)
            Debug.LogWarning("PathfindingDemo: tag your camera as MainCamera.");

        return true;
    }

    void BuildGrid()
    {
        ClearGrid();

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                GameObject instance = Instantiate(tilePrefab, CellToWorld(cell), Quaternion.identity, _cellsParent);
                instance.name = $"Cell_{x}_{y}";
                _cells[cell] = instance.GetComponent<SpriteRenderer>();
                SetCellColor(cell, baseColor);
            }
        }
    }

    void ClearGrid()
    {
        _cells.Clear();

        if (_cellsParent == null)
            return;

        for (int i = _cellsParent.childCount - 1; i >= 0; i--)
            Destroy(_cellsParent.GetChild(i).gameObject);
    }

    void FrameCamera()
    {
        if (_camera == null)
            return;

        _camera.orthographic = true;

        Vector3 center = CellToWorld(new Vector2Int(gridWidth / 2, gridHeight / 2));
        _camera.transform.position = new Vector3(center.x, center.y, -10f);
        _camera.orthographicSize = gridHeight * 0.5f + 1.5f;
    }

    // ------------------------------------------------------------------
    // Player input
    // ------------------------------------------------------------------

    bool CanEditMap() =>
        _phase == DemoPhase.Editing || _phase == DemoPhase.ShowingStats;

    void OnMapEdited()
    {
        if (_phase != DemoPhase.ShowingStats)
            return;

        _phase = DemoPhase.Editing;
        ClearSearchVisuals();
        SetStatus("Map updated. Press Space to run A*.");
    }

    void HandleLeftClick()
    {
        if (!TryGetCellFromPointer(out Vector2Int cell))
            return;

        if (!IsWalkableCell(cell) && cell != _start && cell != _target)
            return;

        if (_start.x < 0)
        {
            _start = cell;
            PaintCell(cell);
            OnMapEdited();
            SetStatus("Start set. LMB again to set the target.");
            return;
        }

        if (_target.x < 0 && cell != _start)
        {
            _target = cell;
            PaintCell(cell);
            OnMapEdited();
            SetStatus("Target set. RMB for obstacles, then Space for A*.");
            return;
        }

        if (cell == _start)
            return;

        if (_target.x >= 0)
            PaintCell(_target);

        _target = cell;
        PaintCell(cell);
        OnMapEdited();
        SetStatus("Target moved. Press Space to run A*.");
    }

    void HandleRightClick()
    {
        if (!TryGetCellFromPointer(out Vector2Int cell))
            return;

        if (cell == _start || cell == _target)
            return;

        if (_obstacles.Contains(cell))
        {
            _obstacles.Remove(cell);
            SetStatus("Obstacle removed.");
        }
        else
        {
            _obstacles.Add(cell);
            SetStatus("Obstacle placed.");
        }

        PaintCell(cell);
        OnMapEdited();
    }

    bool TryGetCellFromPointer(out Vector2Int cell)
    {
        cell = default;

        if (_camera == null)
            return false;

        Vector2 screen = Mouse.current.position.ReadValue();
        float depth = Mathf.Abs(_camera.transform.position.z);
        Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        Vector3Int gridCell = grid.WorldToCell(world);

        if (gridCell.x < 0 || gridCell.x >= gridWidth || gridCell.y < 0 || gridCell.y >= gridHeight)
            return false;

        cell = new Vector2Int(gridCell.x, gridCell.y);
        return true;
    }

    static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    // ------------------------------------------------------------------
    // Search visualization
    // ------------------------------------------------------------------

    IEnumerator AnimateSearch(SearchResult result)
    {
        foreach (Vector2Int cell in result.ExpansionOrder)
        {
            if (cell != _start && cell != _target)
                SetCellColor(cell, visitedColor);

            yield return new WaitForSeconds(stepDelay);
        }

        if (!result.Found)
            yield break;

        foreach (Vector2Int cell in result.Path)
        {
            if (cell != _start && cell != _target)
                SetCellColor(cell, pathColor);
        }
    }

    void ClearSearchVisuals()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
                PaintCell(new Vector2Int(x, y));
        }
    }

    void PaintCell(Vector2Int cell)
    {
        Color color = baseColor;

        if (cell == _start)
            color = startColor;
        else if (cell == _target)
            color = targetColor;
        else if (_obstacles.Contains(cell))
            color = obstacleColor;

        SetCellColor(cell, color);
    }

    void SetCellColor(Vector2Int cell, Color color)
    {
        if (_cells.TryGetValue(cell, out SpriteRenderer renderer))
            renderer.color = color;
    }

    // ------------------------------------------------------------------
    // Grid model sync
    // ------------------------------------------------------------------

    void RebuildGridData()
    {
        _gridData = new Grid2D(gridWidth, gridHeight);

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                _gridData.SetWalkable(x, y, IsWalkableCell(cell));
            }
        }
    }

    bool IsWalkableCell(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
            return false;

        if (cell == _start || cell == _target)
            return true;

        return !_obstacles.Contains(cell);
    }

    Vector3 CellToWorld(Vector2Int cell) =>
        grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));

    // ------------------------------------------------------------------
    // UI
    // ------------------------------------------------------------------

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
