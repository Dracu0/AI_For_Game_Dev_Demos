using System;

// The smallest useful behavior tree: five classes, no editor, no assets.
// A tree is ticked from the root every frame. Each node answers how it went with a Status.
namespace SimpleBT
{
    public enum Status
    {
        Success,    // done, and it worked
        Failure,    // done, and it did not work (or the question's answer is "no")
        Running,    // still busy: tick me again next frame
    }

    public abstract class Node
    {
        public abstract Status Tick();
    }

    // "Try these in order until one does not fail." The first child that succeeds or is running wins.
    // Children higher in the list have priority: put the most urgent behavior first.
    public class Selector : Node
    {
        private readonly Node[] _children;

        public Selector(params Node[] children) => _children = children;

        public override Status Tick()
        {
            foreach (Node child in _children)
            {
                Status status = child.Tick();
                if (status != Status.Failure) return status;
            }
            return Status.Failure;
        }
    }

    // "Do these in order, stop at the first one that does not succeed."
    // Typical use: a Condition followed by the Action it guards.
    public class Sequence : Node
    {
        private readonly Node[] _children;

        public Sequence(params Node[] children) => _children = children;

        public override Status Tick()
        {
            foreach (Node child in _children)
            {
                Status status = child.Tick();
                if (status != Status.Success) return status;
            }
            return Status.Success;
        }
    }

    // A question: Success when true, Failure when false. Never Running.
    public class Condition : Node
    {
        private readonly Func<bool> _test;

        public Condition(Func<bool> test) => _test = test;

        public override Status Tick() => _test() ? Status.Success : Status.Failure;
    }

    // A leaf that does something. The method decides what Status to report.
    public class Action : Node
    {
        private readonly Func<Status> _run;

        public Action(Func<Status> run) => _run = run;

        public override Status Tick() => _run();
    }
}
