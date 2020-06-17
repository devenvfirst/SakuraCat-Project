// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a view for choosing between a set of choices.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ChoiceHandlerPanel : ScriptableUIBehaviour, IManagedUI
    {
        [System.Serializable]
        public class GameState
        {
            public bool RemoveAllButtonsPending;
            // Saving buttons seperately from handler actor choices, as they're destroyed idependently.
            public List<ChoiceState> Buttons = new List<ChoiceState>();
        }

        /// <summary>
        /// Invoked when one of active choices are choosen.
        /// </summary>
        public event Action<ChoiceState> OnChoice;

        protected RectTransform ButtonsContainer => buttonsContainer;
        protected ChoiceHandlerButton DefaultButtonPrefab => defaultButtonPrefab;
        protected bool FocusChoiceButtons => focusChoiceButtons;

        [Tooltip("Container that will hold spawned choice buttons.")]
        [SerializeField] private RectTransform buttonsContainer = null;
        [Tooltip("Button prototype to use by default.")]
        [SerializeField] private ChoiceHandlerButton defaultButtonPrefab = null;
        [Tooltip("Whether to focus added choice buttons for keyboard and gamepad control.")]
        [SerializeField] private bool focusChoiceButtons = true;

        private readonly List<ChoiceHandlerButton> choiceButtons = new List<ChoiceHandlerButton>();
        private IStateManager stateManager;
        private IBacklogUI backlogUI;
        private bool removeAllButtonsPending;

        public virtual UniTask InitializeAsync () => UniTask.CompletedTask;

        UniTask IManagedUI.ChangeVisibilityAsync (bool visible, float? duration)
        {
            Debug.LogError("@showUI and @hideUI commands can't be used with choice handlers; use @show/hide commands instead");
            return UniTask.CompletedTask;
        }

        public virtual void AddChoiceButton (ChoiceState choice)
        {
            if (removeAllButtonsPending)
            {
                removeAllButtonsPending = false;
                RemoveAllChoiceButtons();
            }

            if (choiceButtons.Any(b => b.ChoiceState.Id == choice.Id)) return; // Could happen on rollback.

            var choicePrefab = string.IsNullOrWhiteSpace(choice.ButtonPath) ? defaultButtonPrefab : Resources.Load<ChoiceHandlerButton>(choice.ButtonPath);
            if (!choicePrefab)
            {
                Debug.LogError($"Failed to add `{choice.ButtonPath}` choice button. Make sure the button prefab is stored in a `Resources` folder of the project.");
                return;
            }
            var choiceButton = Instantiate(choicePrefab);
            choiceButton.transform.SetParent(buttonsContainer, false);
            choiceButton.Initialize(choice);
            choiceButton.OnButtonClicked += () => OnChoice?.Invoke(choice);

            if (backlogUI != null)
            {
                choiceButton.OnButtonClicked += () => backlogUI.AddChoice(choiceButtons
                    .Select(b => new Tuple<string, bool>(b.ChoiceState.Summary, b.ChoiceState.Id == choice.Id)).ToList());
            }

            if (choice.OverwriteButtonPosition)
                choiceButton.transform.localPosition = choice.ButtonPosition;

            choiceButtons.Add(choiceButton);

            if (focusChoiceButtons)
            {
                switch (FocusModeType)
                {
                    case FocusMode.Visibility:
                        if (EventSystem.current)
                            EventSystem.current.SetSelectedGameObject(choiceButton.gameObject);
                        break;
                    case FocusMode.Navigation:
                        FocusOnNavigation = choiceButton.gameObject;
                        break;
                }
            }
        }

        public virtual void RemoveChoiceButton (string id)
        {
            var buttons = choiceButtons.FindAll(c => c.ChoiceState.Id == id);
            if (buttons is null || buttons.Count == 0) return;

            foreach (var button in buttons)
            {
                if (button) Destroy(button.gameObject);
                choiceButtons.Remove(button);
            }
        }

        /// <summary>
        /// Will remove the buttons before the next <see cref="AddChoiceButton(ChoiceState)"/> call.
        /// </summary>
        public virtual void RemoveAllChoiceButtonsDelayed ()
        {
            removeAllButtonsPending = true;
        }

        public virtual void RemoveAllChoiceButtons ()
        {
            for (int i = 0; i < choiceButtons.Count; i++)
                Destroy(choiceButtons[i].gameObject);
            choiceButtons.Clear();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(defaultButtonPrefab, buttonsContainer);

            stateManager = Engine.GetService<IStateManager>();
            backlogUI = Engine.GetService<IUIManager>().GetUI<IBacklogUI>();
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

        private void SerializeState (GameStateMap stateMap)
        {
            var state = new GameState() {
                RemoveAllButtonsPending = removeAllButtonsPending,
                Buttons = choiceButtons.Select(b => b.ChoiceState).ToList()
            };
            stateMap.SetState(state, name);
        }

        private UniTask DeserializeState (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>(name);
            if (state is null) return UniTask.CompletedTask;

            var existingButtonIds = choiceButtons.Select(b => b.ChoiceState.Id).ToList();
            foreach (var buttonId in existingButtonIds)
                if (!state.Buttons.Any(s => s.Id == buttonId))
                    RemoveChoiceButton(buttonId);

            foreach (var buttonState in state.Buttons)
                if (!choiceButtons.Any(b => b.ChoiceState == buttonState))
                    AddChoiceButton(buttonState);

            removeAllButtonsPending = state.RemoveAllButtonsPending;

            return UniTask.CompletedTask;
        }
    }
}
