using System;
using System.Collections.Generic;

namespace Pathfinding
{
    /// <summary>
    /// Always returns the item with the smallest priority value first.
    /// Used as the "open set" in Dijkstra and A*.
    /// </summary>
    public sealed class PriorityQueue<TItem>
    {
        // ------------------------------------------------------------------
        // Stored entry
        // ------------------------------------------------------------------

        struct Entry
        {
            public float Priority;
            public long Order;
            public TItem Item;
        }

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        readonly List<Entry> _items = new List<Entry>();
        long _order;

        public bool IsEmpty => _items.Count == 0;

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public void Enqueue(TItem item, float priority)
        {
            var entry = new Entry
            {
                Priority = priority,
                Order = _order++,
                Item = item
            };

            _items.Add(entry);
            BubbleUp(_items.Count - 1);
        }

        public TItem Dequeue()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("Priority queue is empty.");

            Entry best = _items[0];
            Entry last = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);

            if (_items.Count > 0)
            {
                _items[0] = last;
                BubbleDown(0);
            }

            return best.Item;
        }

        // ------------------------------------------------------------------
        // Min-heap maintenance
        // ------------------------------------------------------------------

        void BubbleUp(int index)
        {
            Entry moving = _items[index];

            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (!IsHigherPriority(moving, _items[parent]))
                    break;

                _items[index] = _items[parent];
                index = parent;
            }

            _items[index] = moving;
        }

        void BubbleDown(int index)
        {
            Entry moving = _items[index];

            while (true)
            {
                int left = index * 2 + 1;
                int right = left + 1;

                if (left >= _items.Count)
                    break;

                int bestChild = left;
                if (right < _items.Count && IsHigherPriority(_items[right], _items[left]))
                    bestChild = right;

                if (!IsHigherPriority(_items[bestChild], moving))
                    break;

                _items[index] = _items[bestChild];
                index = bestChild;
            }

            _items[index] = moving;
        }

        static bool IsHigherPriority(Entry a, Entry b)
        {
            if (a.Priority != b.Priority)
                return a.Priority < b.Priority;

            return a.Order < b.Order;
        }
    }
}
