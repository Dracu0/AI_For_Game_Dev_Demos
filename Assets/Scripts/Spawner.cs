using UnityEngine;

public class Spawner : MonoBehaviour
{
    // Static = one shared copy. Any boid can reach it with Spawner.Instance
    // without needing a reference dragged in.
    public static Spawner Instance;

    [Header("Spawning (read once at start)")]
    public Boid prefab;            // the boid prefab to create
    public int count = 60;         // how many boids
    public float spawnRadius = 4f; // boids appear randomly inside this circle

    [Header("Movement")]
    public float maxSpeed = 3f;    // top speed (units per second)
    public float maxForce = 6f;    // how sharply a boid can turn/accelerate

    [Header("Flocking")]
    public float perception = 2f;        // neighbors farther than this are ignored
    public float separationRadius = 0.8f;// closer than this = "too close", push away
    [Range(0f, 5f)] public float wSep = 1.5f; // weight: separation
    [Range(0f, 5f)] public float wAli = 1f;   // weight: alignment
    [Range(0f, 5f)] public float wCoh = 1f;   // weight: cohesion

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask;       // which layer counts as an obstacle (your tilemap)
    public float lookAhead = 1.5f;       // how far ahead a boid checks for walls
    public float boidRadius = 0.2f;      // how "fat" the boid is when checking walls
    [Range(0f, 10f)] public float wAvoid = 3f; // weight: avoidance

    [Header("Area")]
    public Vector2 bounds = new Vector2(9f, 5f); // leaving this box wraps to the other side

    void Awake()
    {
        Instance = this; // Awake runs before any Start, so boids can use it safely
    }

    void Start()
    {
        for (int i = 0; i < count; i++)
        {
            // random point inside a circle, then create a boid there
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }
}