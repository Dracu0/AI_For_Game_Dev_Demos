using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Dijkstra vs A* on the same grid. Draw walls, press Space, and watch how many cells each one explores.
//   Left mouse (drag)  paint / erase walls
//   Right mouse        move the goal
//   S                  move the start to the mouse
//   Space              run Dijkstra, then A*, and compare
public class GridPathfindingDemo : MonoBehaviour
{
    [SerializeField] private int width = 20;
    [SerializeField] private int height = 12;
    [SerializeField] private float stepDelay = 0.01f;     // seconds per expanded cell in the animation
    [SerializeField] private TMP_Text status;

    [Header("Colors")]
    [SerializeField] private Color floorColor = new(0.85f, 0.85f, 0.85f);
    [SerializeField] private Color wallColor = new(0.2f, 0.2f, 0.25f);
    [SerializeField] private Color startColor = Color.green;
    [SerializeField] private Color goalColor = Color.red;
    [SerializeField] private Color visitedColor = new(0.35f, 0.6f, 1f);
    [SerializeField] private Color pathColor = Color.yellow;

    private Grid2D _grid;
    private SpriteRenderer[,] _tiles;
    private Vector2Int _start, _goal;
    private Camera _cam;
    private bool _paintWalkable;
    private Coroutine _running;

    private void Start()
    {
        _cam = Camera.main;
        _grid = new Grid2D(width, height);
        _tiles = new SpriteRenderer[width, height];
        _start = new Vector2Int(1, 1);
        _goal = new Vector2Int(width - 2, height - 2);

        Sprite square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var tile = new GameObject($"Cell {x},{y}");
            tile.transform.SetParent(transform);
            tile.transform.position = new Vector3(x, y, 0f);
            tile.transform.localScale = Vector3.one * 0.92f;
            _tiles[x, y] = tile.AddComponent<SpriteRenderer>();
            _tiles[x, y].sprite = square;
        }

        _cam.orthographic = true;
        _cam.transform.position = new Vector3((width - 1) / 2f, (height - 1) / 2f, -10f);
        _cam.orthographicSize = Mathf.Max(height / 2f + 1f, (width / 2f + 1f) / _cam.aspect);

        Repaint();
        SetStatus("Draw walls with the left mouse, then press Space.");
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Compare());
            return;
        }

        if (_running != null || !MouseCell(out Vector2Int cell)) return;

        if (Mouse.current.leftButton.wasPressedThisFrame) _paintWalkable = !_grid.IsWalkable(cell.x, cell.y);
        if (Mouse.current.leftButton.isPressed && cell != _start && cell != _goal) _grid.SetWalkable(cell.x, cell.y, _paintWalkable);
        if (Mouse.current.rightButton.wasPressedThisFrame && _grid.IsWalkable(cell.x, cell.y)) _goal = cell;
        if (Keyboard.current.sKey.wasPressedThisFrame && _grid.IsWalkable(cell.x, cell.y)) _start = cell;

        Repaint();
    }

    private IEnumerator Compare()
    {
        SearchResult dijkstra = PathSolver.RunDijkstra(_grid, _start, _goal);
        yield return Animate("Dijkstra", dijkstra);
        yield return new WaitForSeconds(1f);

        SearchResult aStar = PathSolver.RunAStar(_grid, _start, _goal);
        yield return Animate("A*", aStar);

        SetStatus(!dijkstra.Found
            ? "No path: the goal is walled off."
            : $"Same cost ({dijkstra.Cost} vs {aStar.Cost}).  Cells explored: Dijkstra {dijkstra.NodesExpanded}, A* {aStar.NodesExpanded}.");
        _running = null;
    }

    private IEnumerator Animate(string name, SearchResult result)
    {
        Repaint();
        SetStatus($"{name}: searching...");

        foreach (Vector2Int cell in result.ExpansionOrder)
        {
            if (cell != _start && cell != _goal) _tiles[cell.x, cell.y].color = visitedColor;
            if (stepDelay > 0f) yield return new WaitForSeconds(stepDelay);
        }
        foreach (Vector2Int cell in result.Path)
            if (cell != _start && cell != _goal) _tiles[cell.x, cell.y].color = pathColor;

        SetStatus(result.Found ? $"{name}: cost {result.Cost}, explored {result.NodesExpanded} cells." : $"{name}: no path.");
    }

    private void Repaint()
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            _tiles[x, y].color = _grid.IsWalkable(x, y) ? floorColor : wallColor;

        _tiles[_start.x, _start.y].color = startColor;
        _tiles[_goal.x, _goal.y].color = goalColor;
    }

    private bool MouseCell(out Vector2Int cell)
    {
        Vector3 world = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        cell = new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
        return _grid.InBounds(cell.x, cell.y);
    }

    private void SetStatus(string message)
    {
        if (status != null) status.text = message;
    }
}
