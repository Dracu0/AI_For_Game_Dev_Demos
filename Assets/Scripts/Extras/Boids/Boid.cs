using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    // Every boid adds itself to this shared list, so each boid can
    // look at all the others without any Find calls.
    public static List<Boid> All = new List<Boid>();

    Vector2 velocity;                  // current direction AND speed in one value
    Spawner S => Spawner.Instance;     // shortcut so we can write S.maxSpeed

    void OnEnable() { All.Add(this); }      // join the list when created
    void OnDisable() { All.Remove(this); }   // leave it when destroyed

    void Start()
    {
        // start moving in a random direction at full speed
        velocity = Random.insideUnitCircle.normalized * S.maxSpeed;
    }

    void Update()
    {
        // 1. LOOK AT NEIGHBORS ------------------------------------------
        Vector2 myPos = transform.position;
        Vector2 sumVelocity = Vector2.zero;  // used for alignment
        Vector2 sumPosition = Vector2.zero;  // used for cohesion
        Vector2 separation = Vector2.zero;  // used for separation
        int neighbors = 0;

        foreach (Boid other in All)
        {
            if (other == this) continue;                     // skip myself

            Vector2 offset = (Vector2)other.transform.position - myPos; // me -> them
            float dist = offset.magnitude;
            if (dist > S.perception) continue;               // too far to see

            neighbors++;
            sumVelocity += other.velocity;
            sumPosition += (Vector2)other.transform.position;

            if (dist < S.separationRadius && dist > 0.0001f)
                separation -= offset / (dist * dist);        // push away; closer = stronger
        }

        // 2. COMBINE THE RULES INTO ONE ACCELERATION ----------------------
        // Each rule gives a "steering force", multiplied by its weight.
        Vector2 accel = separation * S.wSep;

        if (neighbors > 0)
        {
            Vector2 avgVelocity = sumVelocity / neighbors;   // average heading
            Vector2 center = sumPosition / neighbors;   // average position

            accel += Steer(avgVelocity) * S.wAli;       // alignment: match their heading
            accel += Steer(center - myPos) * S.wCoh;       // cohesion: go toward their center
        }

        accel += Avoid() * S.wAvoid;                         // avoidance: don't hit walls

        // 3. MOVE ---------------------------------------------------------
        velocity += accel * Time.deltaTime;                       // accelerate
        velocity = Vector2.ClampMagnitude(velocity, S.maxSpeed);  // cap the speed
        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (velocity.sqrMagnitude > 0.01f)
            transform.up = velocity;                         // sprite tip faces movement

        Wrap();
    }

    // Turns a direction into a steering force.
    // desired velocity - current velocity = the change needed to get there.
    Vector2 Steer(Vector2 direction)
    {
        Vector2 desired = direction.normalized * S.maxSpeed;       // where I WANT to go
        return Vector2.ClampMagnitude(desired - velocity, S.maxForce); // limit how hard I can turn
    }

    // Looks ahead for a wall and returns a force pointing away from it.
    Vector2 Avoid()
    {
        if (velocity.sqrMagnitude < 0.0001f) return Vector2.zero;
        Vector2 dir = velocity.normalized;

        // CircleCast = a thick ray: "is there an obstacle within lookAhead in front of me?"
        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position, S.boidRadius, dir, S.lookAhead, S.obstacleMask);

        if (!hit) return Vector2.zero;                       // path is clear

        float urgency = 1f - hit.distance / S.lookAhead;     // 0 = far away, 1 = touching

        // hit.normal points away from the wall. Adding a sideways direction
        // makes boids hitting a wall head-on turn instead of just bouncing back.
        Vector2 side = new Vector2(-dir.y, dir.x);           // 90 degrees from my direction
        float sign = Vector2.Dot(hit.normal, side) >= 0 ? 1f : -1f;
        Vector2 away = (hit.normal + side * sign).normalized;

        return away * S.maxForce * urgency;
    }

    // Leaving the box on one side puts you on the opposite side.
    void Wrap()
    {
        Vector3 p = transform.position;
        Vector2 b = S.bounds;
        if (p.x > b.x) p.x = -b.x; else if (p.x < -b.x) p.x = b.x;
        if (p.y > b.y) p.y = -b.y; else if (p.y < -b.y) p.y = b.y;
        transform.position = p;
    }
}