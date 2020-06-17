// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Allows to listen for events when an unlockable item managed by <see cref="IUnlockableManager"/> is updated.
    /// </summary>
    public class UnlockableTrigger : MonoBehaviour
    {
        [Serializable]
        private class UnlockedStateChangedEvent : UnityEvent<bool> { }

        /// <summary>
        /// Invoked when unlocked state of the listened unlockable item is changed.
        /// </summary>
        public event Action<bool> OnUnlockedStateChanged;

        /// <summary>
        /// ID of the unlockable item to listen for.
        /// </summary>
        public virtual string UnlockableItemId { get => unlockableItemId; set => unlockableItemId = value; }

        protected IUnlockableManager UnlockableManager => Engine.GetService<IUnlockableManager>();

        [Tooltip("ID of the unlockable item to listen for.")]
        [SerializeField] private string unlockableItemId = default;
        [Tooltip("Invoked when unlocked state of the listened unlockable item is changed; also invoked when the component is started.")]
        [SerializeField] private UnlockedStateChangedEvent onUnlockedStateChanged = default;

        protected virtual void OnEnable ()
        {
            Engine.OnInitializationFinished += Initialize;
            if (Engine.Initialized) Initialize();
        }

        protected virtual void OnDisable ()
        {
            Engine.OnInitializationFinished -= Initialize;
            if (UnlockableManager != null)
                UnlockableManager.OnItemUpdated -= HandleItemUpdated;
        }

        protected virtual void Initialize ()
        {
            UnlockableManager.OnItemUpdated -= HandleItemUpdated;
            UnlockableManager.OnItemUpdated += HandleItemUpdated;
            
            var unlocked = UnlockableManager.ItemUnlocked(UnlockableItemId);
            OnUnlockedStateChanged?.Invoke(unlocked);
            onUnlockedStateChanged?.Invoke(unlocked);
        }

        protected virtual void HandleItemUpdated (UnlockableItemUpdatedArgs args)
        {
            if (!args.Id.EqualsFastIgnoreCase(UnlockableItemId)) return;

            OnUnlockedStateChanged?.Invoke(args.Unlocked);
            onUnlockedStateChanged?.Invoke(args.Unlocked);
        }
    }
}
