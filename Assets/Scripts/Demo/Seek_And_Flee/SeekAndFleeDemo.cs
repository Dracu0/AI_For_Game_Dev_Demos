using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Side-by-side Seek vs Flee demo using the shared steering behaviours.
///
/// Controls:
///   LMB - move the target / threat
///   R   - reset both agents to their spawn positions
/// </summary>
public class SeekAndFleeDemo : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector settings
    // ------------------------------------------------------------------

    [Header("Scene")]
    [SerializeField] GameObject spritePrefab;

    [Header("Movement")]
    [SerializeField] float maxSpeed = 4f;
    [SerializeField] float maxForce = 8f;

    [Header("Layout")]
    [SerializeField] Vector2 seekerSpawn = new Vector2(-6f, 0f);
    [SerializeField] Vector2 fleerSpawn = new Vector2(6f, 0f);
    [SerializeField] Vector2 targetSpawn = new Vector2(0f, 0f);

    [Header("Colors")]
    [SerializeField] Color seekerColor = Color.green;
    [SerializeField] Color fleerColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] Color targetColor = Color.red;

    [Header("UI (optional)")]
    [SerializeField] TMP_Text statusText;

    // ------------------------------------------------------------------
    // Runtime state
    // ------------------------------------------------------------------

    SteeringAgent _seeker;
    SteeringAgent _fleer;
    Transform _target;
    Camera _camera;

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    void Awake()
    {
        _camera = Camera.main;

        if (!ValidateSetup())
            return;

        HideLegacyPathfindingObjects();
        BuildScene();
        FrameCamera();
        SetStatus(
            "Seek (green) moves toward the target.\n" +
            "Flee (blue) moves away from the same target.\n" +
            "LMB: move target. R: reset agents.");
    }

    void Update()
    {
        if (_seeker == null || _fleer == null || _target == null)
            return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ResetAgents();

        if (IsPointerOverUI())
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryMoveTarget();

        Vector2 targetPosition = _target.position;
        _seeker.SeekToward(targetPosition);
        _fleer.FleeFrom(targetPosition);
    }

    // ------------------------------------------------------------------
    // Scene setup
    // ------------------------------------------------------------------

    bool ValidateSetup()
    {
        if (spritePrefab == null)
        {
            Debug.LogError("SeekAndFleeDemo: assign the sprite prefab.");
            return false;
        }

        if (spritePrefab.GetComponent<SpriteRenderer>() == null)
        {
            Debug.LogError("SeekAndFleeDemo: the sprite prefab needs a SpriteRenderer.");
            return false;
        }

        if (_camera == null)
            Debug.LogWarning("SeekAndFleeDemo: tag your camera as MainCamera.");

        return true;
    }

    void HideLegacyPathfindingObjects()
    {
        GameObject grid = GameObject.Find("Grid");
        if (grid != null)
            grid.SetActive(false);
    }

    void BuildScene()
    {
        _seeker = CreateAgent("Seeker", seekerSpawn, seekerColor);
        _fleer = CreateAgent("Fleer", fleerSpawn, fleerColor);
        _target = CreateMarker("Target", targetSpawn, targetColor);
    }

    SteeringAgent CreateAgent(string agentName, Vector2 spawnPosition, Color color)
    {
        GameObject instance = Instantiate(spritePrefab, spawnPosition, Quaternion.identity, transform);
        instance.name = agentName;
        instance.GetComponent<SpriteRenderer>().color = color;

        SteeringAgent agent = instance.AddComponent<SteeringAgent>();
        agent.Configure(maxSpeed, maxForce);
        agent.ResetMotion();
        return agent;
    }

    Transform CreateMarker(string markerName, Vector2 spawnPosition, Color color)
    {
        GameObject instance = Instantiate(spritePrefab, spawnPosition, Quaternion.identity, transform);
        instance.name = markerName;
        instance.GetComponent<SpriteRenderer>().color = color;
        return instance.transform;
    }

    void FrameCamera()
    {
        if (_camera == null)
            return;

        _camera.orthographic = true;
        _camera.transform.position = new Vector3(0f, 0f, -10f);
        _camera.orthographicSize = 6f;
    }

    // ------------------------------------------------------------------
    // Input
    // ------------------------------------------------------------------

    void TryMoveTarget()
    {
        if (_camera == null || Mouse.current == null)
            return;

        Vector2 screen = Mouse.current.position.ReadValue();
        float depth = Mathf.Abs(_camera.transform.position.z);
        Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        _target.position = new Vector3(world.x, world.y, 0f);
    }

    void ResetAgents()
    {
        _seeker.transform.position = seekerSpawn;
        _fleer.transform.position = fleerSpawn;
        _seeker.ResetMotion();
        _fleer.ResetMotion();
        SetStatus("Agents reset. LMB: move target.");
    }

    static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    // ------------------------------------------------------------------
    // UI
    // ------------------------------------------------------------------

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
