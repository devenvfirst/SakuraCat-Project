// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;

namespace Naninovel
{
    /// <inheritdoc cref="IUnlockableManager"/>
    [InitializeAtRuntime]
    public class UnlockableManager : IUnlockableManager, IStatefulService<GlobalStateMap>
    {
        /// <summary>
        /// Serializable dictionary, representing unlockable item ID to its unlocked state map.
        /// </summary>
        [Serializable]
        public class UnlockablesMap : SerializableMap<string, bool>
        {
            public UnlockablesMap () : base(StringComparer.OrdinalIgnoreCase) { }
            public UnlockablesMap (UnlockablesMap map) : base(map, StringComparer.OrdinalIgnoreCase) { }
        }

        [Serializable]
        public class GlobalState
        {
            public UnlockablesMap UnlockablesMap = new UnlockablesMap();
        }

        public event Action<UnlockableItemUpdatedArgs> OnItemUpdated;

        public UnlockablesConfiguration Configuration { get; }

        private UnlockablesMap unlockablesMap;

        public UnlockableManager (UnlockablesConfiguration config)
        {
            Configuration = config;
            unlockablesMap = new UnlockablesMap();
        }

        public UniTask InitializeServiceAsync () => UniTask.CompletedTask;

        public void ResetService () { }

        public void DestroyService () { }

        public void SaveServiceState (GlobalStateMap stateMap)
        {
            var globalState = new GlobalState {
                UnlockablesMap = new UnlockablesMap(unlockablesMap)
            };
            stateMap.SetState(globalState);
        }

        public UniTask LoadServiceStateAsync (GlobalStateMap stateMap)
        {
            var state = stateMap.GetState<GlobalState>();
            if (state is null) return UniTask.CompletedTask;

            unlockablesMap = new UnlockablesMap(state.UnlockablesMap);
            return UniTask.CompletedTask;
        }

        public bool ItemUnlocked (string itemId) => unlockablesMap.TryGetValue(itemId, out var item) && item;

        public void SetItemUnlocked (string itemId, bool unlocked)
        {
            if (unlocked && ItemUnlocked(itemId)) return;
            if (!unlocked && unlockablesMap.ContainsKey(itemId) && !ItemUnlocked(itemId)) return;

            var added = unlockablesMap.ContainsKey(itemId);
            unlockablesMap[itemId] = unlocked;
            OnItemUpdated?.Invoke(new UnlockableItemUpdatedArgs(itemId, unlocked, added));
        }

        public void UnlockItem (string itemId) => SetItemUnlocked(itemId, true);

        public void LockItem (string itemId) => SetItemUnlocked(itemId, false);

        public Dictionary<string, bool> GetAllItems () => unlockablesMap.ToDictionary(kv => kv.Key, kv => kv.Value);

        public void UnlockAllItems ()
        {
            foreach (var itemId in unlockablesMap.Keys)
                UnlockItem(itemId);
        }

        public void LockAllItems ()
        {
            foreach (var itemId in unlockablesMap.Keys)
                LockItem(itemId);
        }
    }
}
