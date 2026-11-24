using System;
using System.Collections.Generic;
using UnityEngine;

// A GOAP agent: look at the world, ask the planner for the cheapest way to reach the goal, then carry the plan out.
// Nothing in here knows about axes or fish. All of that is data configured in the Inspector.
public class GoapAgent : MonoBehaviour
{
    // "This fact stops being true a while after it became true" (a fire burns out, hunger comes back).
    [Serializable]
    public class Expiry
    {
        public Fact fact;
        public float seconds = 20f;
        [NonSerialized] public float age;
    }

    public Fact goal = Fact.Fed;             // the facts we want to be true
    public Fact state;                       // the facts that are true right now (as authored: the starting world)
    public List<GoapAction> actions = new();
    public List<Expiry> expiries = new();
    public float moveSpeed = 4f;
    public float reach = 0.8f;               // how close counts as "at the target"

    public IReadOnlyList<GoapAction> Plan => _plan;
    public int Step => _step;                // index of the action being carried out
    public GoapAction Current => _step < _plan.Count ? _plan[_step] : null;
    public bool GoalMet => (state & goal) == goal;

    private readonly List<GoapAction> _plan = new();
    private int _step;
    private float _work;                     // seconds spent on the current action
    private float _retryAt;                  // do not plan again before this time (after a failure)

    private void Update()
    {
        ExpireFacts();

        if (GoalMet) { ClearPlan(); return; }          // nothing to do: idle until the world changes

        if (!PlanStillWorks())                         // no plan yet, or the world changed under it
        {
            ClearPlan();
            if (Time.time < _retryAt) return;

            List<GoapAction> found = GoapPlanner.Plan(state, goal, actions);
            if (found == null)
            {
                Debug.LogWarning($"{name}: no sequence of actions reaches the goal {goal}.", this);
                _retryAt = Time.time + 1f;
                return;
            }
            _plan.AddRange(found);
        }

        GoapAction action = Current;
        if (action.target != null && !InReach(action.target.position)) return;   // still walking there

        _work += Time.deltaTime;
        if (_work < action.duration) return;                                      // still working

        state = action.ApplyTo(state);
        _step++;
        _work = 0f;
    }

    // Flips a fact by hand. The HUD uses it so you can steal the axe or put the fire out and watch the agent re-plan.
    public void Toggle(Fact fact) => state ^= fact;

    // Seconds until a fact fades by itself, or -1 if it is false or never fades.
    public float TimeLeft(Fact fact)
    {
        if ((state & fact) == 0) return -1f;
        foreach (Expiry expiry in expiries)
            if (expiry.fact == fact) return expiry.seconds - expiry.age;
        return -1f;
    }

    // Play the rest of the plan on top of the current world. Every step must be allowed and the goal must be met at the end.
    private bool PlanStillWorks()
    {
        if (_step >= _plan.Count) return false;

        Fact simulated = state;
        for (int i = _step; i < _plan.Count; i++)
        {
            if (!_plan[i].CanRunIn(simulated)) return false;
            simulated = _plan[i].ApplyTo(simulated);
        }
        return (simulated & goal) == goal;
    }

    private void ClearPlan()
    {
        _plan.Clear();
        _step = 0;
        _work = 0f;
    }

    private bool InReach(Vector3 point)
    {
        if (Vector2.Distance(transform.position, point) <= reach) return true;

        transform.position = Vector3.MoveTowards(transform.position, point, moveSpeed * Time.deltaTime);
        return false;
    }

    private void ExpireFacts()
    {
        foreach (Expiry expiry in expiries)
        {
            if ((state & expiry.fact) == 0) { expiry.age = 0f; continue; }

            expiry.age += Time.deltaTime;
            if (expiry.age < expiry.seconds) continue;

            state &= ~expiry.fact;
            expiry.age = 0f;
        }
    }

    // A negative cost would let the planner loop forever, so keep costs sane even if someone types nonsense.
    private void OnValidate()
    {
        foreach (GoapAction action in actions)
        {
            action.cost = Mathf.Max(0f, action.cost);
            action.duration = Mathf.Max(0f, action.duration);
        }
        foreach (Expiry expiry in expiries) expiry.seconds = Mathf.Max(0.1f, expiry.seconds);
    }
}
