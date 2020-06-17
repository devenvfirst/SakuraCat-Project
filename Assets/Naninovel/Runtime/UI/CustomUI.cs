// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// An implementation of <see cref="IManagedUI"/>, that
    /// can be used to create custom user managed UI objects.
    /// </summary>
    public class CustomUI : ScriptableUIBehaviour, IManagedUI
    {
        [System.Serializable]
        public class GameState
        {
            public bool Visible;
        }

        public bool HideOnLoad => hideOnLoad;
        public bool SaveVisibilityState => saveVisibilityState;
        public bool BlockInputWhenVisible => blockInputWhenVisible;
        public bool ModalUI => modalUI;

        protected virtual string[] AllowedSamplers => allowedSamplers;

        [Tooltip("Whether to automatically hide the UI when loading game or resetting state.")]
        [SerializeField] private bool hideOnLoad = true;
        [Tooltip("Whether to preserve visibility of the UI when saving/loading game.")]
        [SerializeField] private bool saveVisibilityState = true;
        [Tooltip("Whether to halt user input processing while the UI is visible.")]
        [SerializeField] private bool blockInputWhenVisible = false;
        [Tooltip("Which input samplers should still be allowed in case the input is blocked while the UI is visible.")]
        [SerializeField] private string[] allowedSamplers = default;
        [Tooltip("Whether to make all the other managed UIs not interactable while the UI is visible.")]
        [SerializeField] private bool modalUI = false;

        private IStateManager stateManager;
        private IInputManager inputManager;
        private IUIManager uiManager;

        public virtual UniTask InitializeAsync () => UniTask.CompletedTask;

        protected override void Awake ()
        {
            base.Awake();

            stateManager = Engine.GetService<IStateManager>();
            inputManager = Engine.GetService<IInputManager>();
            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (hideOnLoad)
            {
                stateManager.OnGameLoadStarted += HandleGameLoadStarted;
                stateManager.OnResetStarted += Hide;
            }

            stateManager.AddOnGameSerializeTask(SerializeState);
            stateManager.AddOnGameDeserializeTask(DeserializeState);

            if (blockInputWhenVisible)
                inputManager.AddBlockingUI(this, AllowedSamplers);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (hideOnLoad && stateManager != null)
            {
                stateManager.OnGameLoadStarted -= HandleGameLoadStarted;
                stateManager.OnResetStarted -= Hide;
            }

            if (stateManager != null)
            {
                stateManager.RemoveOnGameSerializeTask(SerializeState);
                stateManager.RemoveOnGameDeserializeTask(DeserializeState);
            }

            if (blockInputWhenVisible)
                inputManager?.RemoveBlockingUI(this);
        }

        protected virtual void SerializeState (GameStateMap stateMap)
        {
            if (saveVisibilityState)
            {
                var state = new GameState() {
                    Visible = Visible
                };
                stateMap.SetState(state, name);
            }
        }

        protected virtual UniTask DeserializeState (GameStateMap stateMap)
        {
            if (saveVisibilityState)
            {
                var state = stateMap.GetState<GameState>(name);
                if (state is null) return UniTask.CompletedTask;
                Visible = state.Visible;
            }
            return UniTask.CompletedTask;
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            if (modalUI)
                uiManager?.SetModalUI(visible ? this : null);
        }

        private void HandleGameLoadStarted (GameSaveLoadArgs args) => Hide();
    }
}
