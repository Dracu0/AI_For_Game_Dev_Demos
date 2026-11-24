using System;
using UnityEngine;

// An action is pure data: what it needs, what it changes, what it costs and where it happens.
// The planner chains actions by matching one action's effects to the next action's needs.
[Serializable]
public class GoapAction
{
    public string name;
    public Fact needs;              // all of these must be true before the action can start
    public Fact adds;               // these become true when it finishes
    public Fact removes;            // these become false when it finishes
    public float cost = 1f;         // the planner picks the plan with the lowest total cost
    public float duration = 1f;     // seconds of work once the agent has arrived
    public Transform target;        // where it happens (leave empty to act in place)

    public bool CanRunIn(Fact state) => (state & needs) == needs;

    public Fact ApplyTo(Fact state) => (state | adds) & ~removes;
}
