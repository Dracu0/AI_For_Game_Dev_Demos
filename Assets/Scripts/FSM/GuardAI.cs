using UnityEngine;

public class GuardAI : GuardBody
{
    private State _current;

    private void Start() => Change(new PatrolState());

    private void FixedUpdate() => _current.Tick(this);

    public void Change(State next)
    {
        _current = next;
        _current.Enter(this);
    }
}

public abstract class State
{
    public virtual void Enter(GuardAI g) { }
    public abstract void Tick(GuardAI g);
}

public class PatrolState : State
{
    public override void Enter(GuardAI g)
    {
        g.Show("Patrol", Color.green);
        g.GoToNearestPatrolPoint();
    }

    public override void Tick(GuardAI g)
    {
        if (g.DistanceToPlayer <= g.sightRadius)
            g.Change(new ChaseState());
        else if (g.MoveToward(g.PatrolPoint, g.walkSpeed))
            g.NextPatrolPoint();
    }
}

public class ChaseState : State
{
    public override void Enter(GuardAI g) => g.Show("Chase", Color.red);

    public override void Tick(GuardAI g)
    {
        float distance = g.DistanceToPlayer;

        if (distance > g.loseRadius)
            g.Change(new SearchState());
        else if (distance <= g.attackRange)
            g.Change(new AttackState());
        else
            g.MoveToward(g.PlayerPosition, g.chaseSpeed);
    }
}

public class SearchState : State
{
    private float _end;

    public override void Enter(GuardAI g)
    {
        g.Show("Search", Color.yellow);
        g.Stop();
        _end = Time.time + g.searchTime;
    }

    public override void Tick(GuardAI g)
    {
        if (g.DistanceToPlayer <= g.sightRadius)
            g.Change(new ChaseState());
        else if (Time.time >= _end)
            g.Change(new PatrolState());
    }
}

public class AttackState : State
{
    private float _nextStrike;

    public override void Enter(GuardAI g)
    {
        g.Show("Attack", Color.magenta);
        g.Stop();
        _nextStrike = Time.time;                    // a fresh visit strikes straight away
    }

    public override void Tick(GuardAI g)
    {
        // Leave at a bigger distance than we entered at: hysteresis again.
        if (g.DistanceToPlayer > g.attackRange * 1.25f)
        {
            g.Change(new ChaseState());
            return;
        }

        if (Time.time >= _nextStrike)
        {
            _nextStrike = Time.time + g.attackCooldown;
            g.Flash();
        }
    }
}
