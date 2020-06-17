// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Collections;

namespace Naninovel
{
    /// <summary>
    /// A limited-size dropout stack containing <see cref="GameStateMap"/> objects.
    /// </summary>
    public class StateRollbackStack : IEnumerable<GameStateMap>
    {
        [System.Serializable]
        private class SerializedStack
        {
            public List<GameStateMap> List = default;

            public SerializedStack (List<GameStateMap> list)
            {
                List = list;
            }
        }

        /// <summary>
        /// The stack will dropout elements when the capacity is exceeded.
        /// </summary>
        public readonly int Capacity;
        /// <summary>
        /// Number of elements in stack.
        /// </summary>
        public int Count => rollbackList.Count;

        private readonly LinkedList<GameStateMap> rollbackList = new LinkedList<GameStateMap>();

        public StateRollbackStack (int capacity)
        {
            Capacity = capacity;
        }

        public IEnumerator<GameStateMap> GetEnumerator () => rollbackList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator () => rollbackList.GetEnumerator();

        public void Push (GameStateMap item)
        {
            rollbackList.AddFirst(item);

            if (rollbackList.Count > Capacity)
                rollbackList.RemoveLast();
        }

        public GameStateMap Peek () => rollbackList?.Count > 0 ? rollbackList.First?.Value : null;

        public GameStateMap Pop ()
        {
            if (Count == 0) return null;

            var item = rollbackList.First.Value;
            rollbackList.RemoveFirst();
            return item;
        }

        public GameStateMap Pop (Predicate<GameStateMap> predicate)
        {
            var spotFound = false;
            var node = rollbackList.First;
            while (node != null)
            {
                if (predicate(node.Value)) { spotFound = true; break; }
                node = node.Next;
            }
            if (!spotFound) return null;

            while (rollbackList.First != node)
                Pop();

            return Pop();
        }

        public bool Contains (Predicate<GameStateMap> predicate)
        {
            var node = rollbackList.First;
            while (node != null)
            {
                if (predicate(node.Value)) return true;
                node = node.Next;
            }
            return false;
        }

        public void Clear () => rollbackList.Clear();

        public void ForEach (Action<GameStateMap> action)
        {
            foreach (var state in rollbackList)
                action(state);
        }

        public void OverrideFromJson (string json, Action<GameStateMap> @foreach = null)
        {
            Clear();

            if (string.IsNullOrEmpty(json)) return;

            var serializedStack = JsonUtility.FromJson<SerializedStack>(json);
            if (serializedStack?.List is null || serializedStack.List.Count == 0) return;

            foreach (var item in serializedStack.List)
            {
                @foreach?.Invoke(item);
                Push(item);
            }
        }

        public string ToJson (int maxSize, Predicate<GameStateMap> filter = null)
        {
            var filtererdList = rollbackList.Where(s => filter is null || filter(s)).Reverse().ToList();
            var rangeCount = Mathf.Min(maxSize, filtererdList.Count);
            if (rangeCount == 0) return null;

            var list = filtererdList.GetRange(filtererdList.Count - rangeCount, rangeCount);
            var serializedStack = new SerializedStack(list);
            return JsonUtility.ToJson(serializedStack);
        }
    }
}
