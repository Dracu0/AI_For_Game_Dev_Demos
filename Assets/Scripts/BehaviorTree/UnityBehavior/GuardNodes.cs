using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

// Custom nodes for the Unity Behavior package (Window > AI > Behavior), so the same guard can be built as a graph.
// Each node drives the GuardBody on the [Guard] GameObject; link [Guard] to the blackboard's "Self" variable.
//
// Suggested graph (see Docs/behavior-tree.md):
//
//   On Start
//   └ Repeat (Forever)
//      └ Sequence
//         ├ Guard patrols until it spots the player
//         ├ Repeat (While: Guard can still see the player)
//         │  └ Sequence
//         │     ├ Guard chases the player
//         │     └ Guard attacks the player
//         └ Guard searches the area

static class GuardNode
{
    public static GuardBody Body(BlackboardVariable<GameObject> guard)
    {
        GuardBody body = guard?.Value != null ? guard.Value.GetComponent<GuardBody>() : null;
        if (body == null) Debug.LogWarning("Guard node: [Guard] must be a GameObject with a GuardBody (e.g. GuardGraphBody).");
        return body;
    }
}

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Guard Patrols", story: "[Guard] patrols until it spots the player",
    category: "Action/Guard", id: "f8cbe18743a24692912c6a36bf8f7062")]
public partial class GuardPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Guard;
    private GuardBody _body;

    protected override Status OnStart()
    {
        _body = GuardNode.Body(Guard);
        if (_body == null) return Status.Failure;

        _body.Show("Patrol", Color.green);
        _body.GoToNearestPatrolPoint();
        return Status.Running;
    }

    // Success = "I saw someone": the rest of the sequence takes over.
    protected override Status OnUpdate()
    {
        if (_body.DistanceToPlayer <= _body.sightRadius) return Status.Success;

        if (_body.MoveToward(_body.PatrolPoint, _body.walkSpeed)) _body.NextPatrolPoint();
        return Status.Running;
    }
}

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Guard Chases", story: "[Guard] chases the player",
    category: "Action/Guard", id: "64b52c31a0cc4c92a1319d5022beea04")]
public partial class GuardChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Guard;
    private GuardBody _body;

    protected override Status OnStart()
    {
        _body = GuardNode.Body(Guard);
        if (_body == null) return Status.Failure;

        _body.Show("Chase", Color.red);
        return Status.Running;
    }

    // Success = close enough to strike. Failure = the player got away (outside loseRadius).
    protected override Status OnUpdate()
    {
        float distance = _body.DistanceToPlayer;
        if (distance > _body.loseRadius) return Status.Failure;
        if (distance <= _body.attackRange) return Status.Success;

        _body.MoveToward(_body.PlayerPosition, _body.chaseSpeed);
        return Status.Running;
    }

    protected override void OnEnd() => _body?.Stop();
}

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Guard Attacks", story: "[Guard] attacks the player",
    category: "Action/Guard", id: "9e5340f85330473db585e5e648422f40")]
public partial class GuardAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Guard;
    private GuardBody _body;
    private float _nextStrike;

    protected override Status OnStart()
    {
        _body = GuardNode.Body(Guard);
        if (_body == null) return Status.Failure;

        _body.Show("Attack", Color.magenta);
        _body.Stop();
        _nextStrike = Time.time;                    // a fresh attack strikes straight away
        return Status.Running;
    }

    // Success = the player stepped out of reach (leave further out than we entered: hysteresis).
    protected override Status OnUpdate()
    {
        if (_body.DistanceToPlayer > _body.attackRange * 1.25f) return Status.Success;

        if (Time.time >= _nextStrike)
        {
            _nextStrike = Time.time + _body.attackCooldown;
            _body.Flash();
        }
        return Status.Running;
    }
}

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Guard Searches", story: "[Guard] searches the area",
    category: "Action/Guard", id: "d049c7df568041d39135dc3771ea67ec")]
public partial class GuardSearchAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Guard;
    private GuardBody _body;
    private float _end;

    protected override Status OnStart()
    {
        _body = GuardNode.Body(Guard);
        if (_body == null) return Status.Failure;

        _body.Show("Search", Color.yellow);
        _body.Stop();
        _end = Time.time + _body.searchTime;
        return Status.Running;
    }

    // Success either way: after searchTime, or as soon as the player is back in sight
    // (the outer Repeat restarts at Patrol, which then succeeds at once and the chase resumes).
    protected override Status OnUpdate()
    {
        if (_body.DistanceToPlayer <= _body.sightRadius) return Status.Success;
        return Time.time >= _end ? Status.Success : Status.Running;
    }
}

[Serializable, GeneratePropertyBag]
[Condition(name: "Guard Can See Player", story: "[Guard] can still see the player",
    category: "Conditions/Guard", id: "5897efd387e74b6dbaee55235181dafc")]
public partial class GuardCanSeePlayerCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Guard;

    // Uses loseRadius, not sightRadius: once hunting, the guard only gives up when the player is far away.
    public override bool IsTrue()
    {
        GuardBody body = GuardNode.Body(Guard);
        return body != null && body.DistanceToPlayer <= body.loseRadius;
    }
}
