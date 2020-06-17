// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UniRx.Async;
using UnityEngine.EventSystems;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a full-screen invisible UI panel, which blocks UI interaction and all (or a subset of) the input samplers while visible,
    /// but can hide itself and execute (if registered) `onClick` callback when user clicks the panel.
    /// </summary>
    public class ClickThroughPanel : ScriptableUIBehaviour, IManagedUI, IPointerClickHandler
    {
        [System.Serializable]
        public class GameState
        {
            public bool Visible;
            public bool HideOnClick;
            public string[] AllowedSamplers;
        }

        private IInputManager inputManager;
        private IStateManager stateManager;
        private Action onClick;
        private bool hideOnClick;
        private string[] allowedSamplers;

        public virtual UniTask InitializeAsync () => UniTask.CompletedTask;

        public virtual void Show (bool hideOnClick, Action onClick, params string[] allowedSamplers)
        {
            this.hideOnClick = hideOnClick;
            this.onClick = onClick;
            this.allowedSamplers = allowedSamplers;
            Show();
            inputManager.AddBlockingUI(this, allowedSamplers);
        }

        public override void Hide ()
        {
            onClick = null;
            allowedSamplers = null;
            inputManager.RemoveBlockingUI(this);
            base.Hide();
        }

        public virtual void OnPointerClick (PointerEventData eventData)
        {
            onClick?.Invoke();
            if (hideOnClick) Hide();
        }

        protected override void Awake ()
        {
            base.Awake();

            stateManager = Engine.GetService<IStateManager>();
            inputManager = Engine.GetService<IInputManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            stateManager.AddOnGameSerializeTask(SerializeState);
            stateManager.AddOnGameDeserializeTask(DeserializeState);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (stateManager != null)
            {
                stateManager.RemoveOnGameSerializeTask(SerializeState);
                stateManager.RemoveOnGameDeserializeTask(DeserializeState);
            }
        }

        protected virtual void SerializeState (GameStateMap stateMap)
        {
            var state = new GameState() {
                Visible = Visible,
                HideOnClick = hideOnClick,
                AllowedSamplers = allowedSamplers
            };
            stateMap.SetState(state, name);
        }

        protected virtual UniTask DeserializeState (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>(name);
            if (state is null) return UniTask.CompletedTask;

            Hide();

            if (state.Visible) // TODO: Serialize onClick action.
                Show(state.HideOnClick, null, state.AllowedSamplers);

            return UniTask.CompletedTask;
        }
    }
}
