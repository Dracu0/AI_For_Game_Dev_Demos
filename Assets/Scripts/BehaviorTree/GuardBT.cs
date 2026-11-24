using SimpleBT;
using UnityEngine;

// The same guard as the FSM scene (same body, same numbers, same colors), but the brain is a behavior tree.
//
//   Selector                                  (the first branch that does not fail wins)
//   ├─ Sequence: [player in strike range?]  -> Attack
//   ├─ Sequence: [player spotted?]          -> Chase
//   ├─ Sequence: [player just lost?]        -> Search
//   └─ Patrol
//
// Compare with GuardAI (FSM): there are no transitions here. Every physics step the tree is asked again
// from the top, and the highest-priority branch whose condition holds runs. Adding a behavior means
// adding one branch at the right height, without touching the others.
public class GuardBT : GuardBody
{
    private Node _root;
    private string _activity = "";      // which leaf ran last: lets conditions add hysteresis and leaves detect "just started"
    private float _searchUntil;
    private float _nextStrike;

    private void Start()
    {
        _root = new Selector(
            new Sequence(new Condition(PlayerInStrikeRange), new Action(Attack)),
            new Sequence(new Condition(PlayerSpotted), new Action(Chase)),
            new Sequence(new Condition(PlayerJustLost), new Action(Search)),
            new Action(Patrol)
        );
    }

    private void FixedUpdate() => _root.Tick();

    // ---------- Conditions (questions only: no movement, no side effects) ----------

    // Already attacking? Then keep at it a little further out: the same hysteresis as the FSM.
    private bool PlayerInStrikeRange() =>
        DistanceToPlayer <= (_activity == "Attack" ? attackRange * 1.25f : attackRange);

    // Notice the player inside sightRadius, but once hunting only give up outside loseRadius.
    private bool PlayerSpotted() =>
        DistanceToPlayer <= (IsHunting ? loseRadius : sightRadius);

    // We were hunting a moment ago (and the branches above just failed), or we are mid-search.
    private bool PlayerJustLost() =>
        IsHunting || (_activity == "Search" && Time.time < _searchUntil);

    private bool IsHunting => _activity == "Chase" || _activity == "Attack";

    // ---------- Actions ----------

    private Status Attack()
    {
        if (Begin("Attack", Color.magenta)) _nextStrike = Time.time;   // a fresh attack strikes straight away

        Stop();
        if (Time.time >= _nextStrike)
        {
            _nextStrike = Time.time + attackCooldown;
            Flash();
        }
        return Status.Running;
    }

    private Status Chase()
    {
        Begin("Chase", Color.red);
        MoveToward(PlayerPosition, chaseSpeed);
        return Status.Running;
    }

    private Status Search()
    {
        if (Begin("Search", Color.yellow)) _searchUntil = Time.time + searchTime;

        Stop();
        return Status.Running;
    }

    private Status Patrol()
    {
        if (Begin("Patrol", Color.green)) GoToNearestPatrolPoint();

        if (MoveToward(PatrolPoint, walkSpeed)) NextPatrolPoint();
        return Status.Running;
    }

    // A tree has no "Enter": a leaf finds out it just started by noticing a different leaf ran last time.
    // Returns true on that first tick, so the leaf can do its one-off setup.
    private bool Begin(string activity, Color color)
    {
        if (_activity == activity) return false;

        _activity = activity;
        Show(activity, color);
        return true;
    }
}
