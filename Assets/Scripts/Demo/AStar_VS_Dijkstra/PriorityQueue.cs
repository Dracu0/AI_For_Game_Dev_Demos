using System;
using System.Collections.Generic;

namespace Pathfinding
{
    /// <summary>
    /// Always returns the item with the smallest priority first.
    /// Used as the "open set" in Dijkstra and A*.
    ///
    /// This scans the list for the cheapest item: easy to read, fast enough
    /// for the small teaching grid. Games with large maps use a binary heap.
    /// </summary>
    public sealed class PriorityQueue<TItem>
    {
        struct Entry
        {
            public TItem Item;
            public float Priority;
        }

        readonly List<Entry> _items = new List<Entry>();

        public bool IsEmpty => _items.Count == 0;

        public void Enqueue(TItem item, float priority)
        {
            _items.Add(new Entry { Item = item, Priority = priority });
        }

        public TItem Dequeue()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("Priority queue is empty.");

            int best = 0;
            for (int i = 1; i < _items.Count; i++)
            {
                if (_items[i].Priority < _items[best].Priority)
                    best = i;
            }

            TItem item = _items[best].Item;
            _items.RemoveAt(best);
            return item;
        }
    }
}
