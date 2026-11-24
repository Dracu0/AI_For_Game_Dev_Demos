using System.Collections.Generic;

// Finds the cheapest list of actions that turns `start` into a world containing every fact of `goal`.
// It is a uniform-cost search (A* with no heuristic) over world states: tiny, and exact for small action sets.
public static class GoapPlanner
{
    private class Node
    {
        public Fact State;
        public float Cost;          // total cost of the actions taken to get here
        public Node Parent;
        public GoapAction Action;   // the action that led here from Parent
    }

    // Returns null when the goal cannot be reached, and an empty list when it is already met.
    public static List<GoapAction> Plan(Fact start, Fact goal, IReadOnlyList<GoapAction> actions)
    {
        var best = new Dictionary<Fact, Node>();   // cheapest known way to reach each world state
        var open = new List<Node>();               // states still to explore

        var first = new Node { State = start };
        best[start] = first;
        open.Add(first);

        while (open.Count > 0)
        {
            Node node = PopCheapest(open);
            if (best[node.State] != node) continue;                  // a cheaper route to this state was found meanwhile
            if ((node.State & goal) == goal) return Unwind(node);    // cheapest node first, so this is the cheapest plan

            foreach (GoapAction action in actions)
            {
                if (!action.CanRunIn(node.State)) continue;

                Fact next = action.ApplyTo(node.State);
                float cost = node.Cost + action.cost;
                if (best.TryGetValue(next, out Node known) && known.Cost <= cost) continue;

                var child = new Node { State = next, Cost = cost, Parent = node, Action = action };
                best[next] = child;
                open.Add(child);
            }
        }

        return null;
    }

    private static Node PopCheapest(List<Node> open)
    {
        int cheapest = 0;
        for (int i = 1; i < open.Count; i++)
            if (open[i].Cost < open[cheapest].Cost) cheapest = i;

        Node node = open[cheapest];
        open.RemoveAt(cheapest);
        return node;
    }

    private static List<GoapAction> Unwind(Node node)
    {
        var plan = new List<GoapAction>();
        for (; node.Parent != null; node = node.Parent) plan.Add(node.Action);
        plan.Reverse();
        return plan;
    }
}
