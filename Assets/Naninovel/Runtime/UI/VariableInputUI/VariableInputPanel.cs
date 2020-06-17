// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class VariableInputPanel : CustomUI, IVariableInputUI
    {
        [System.Serializable]
        public new class GameState
        {
            public string VariableName;
            public string SummaryText;
            public string InputFieldText;
            public bool PlayOnSubmit;
        }

        protected InputField InputField => inputField;
        protected Text SummaryText => summaryText;
        protected Button SubmitButton => submitButton;
        protected bool ActivateOnShow => activateOnShow;
        protected bool SubmitOnInput => submitOnInput;

        [SerializeField] private InputField inputField = default;
        [SerializeField] private Text summaryText = default;
        [SerializeField] private Button submitButton = default;
        [Tooltip("Whether to automatically select and activate input field when the UI is shown.")]
        [SerializeField] private bool activateOnShow = true;
        [Tooltip("Whether to attempt submit input field value when a `Submit` input is activated.")]
        [SerializeField] private bool submitOnInput = true;

        private IScriptPlayer scriptPlayer;
        private ICustomVariableManager variableManager;
        private IStateManager stateManager;
        private IInputSampler submitInput;
        private string variableName;
        private bool playOnSubmit;

        public virtual void Show (string variableName, string summary, string predefinedValue, bool playOnSubmit)
        {
            this.variableName = variableName;
            this.playOnSubmit = playOnSubmit;
            summaryText.text = summary ?? string.Empty;
            summaryText.gameObject.SetActive(!string.IsNullOrWhiteSpace(summary));
            inputField.text = predefinedValue ?? string.Empty;

            Show();

            if (activateOnShow)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(inputField, summaryText, submitButton);

            scriptPlayer = Engine.GetService<IScriptPlayer>();
            variableManager = Engine.GetService<ICustomVariableManager>();
            stateManager = Engine.GetService<IStateManager>();
            submitInput = Engine.GetService<IInputManager>().GetSubmit();

            submitButton.interactable = false;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            submitButton.onClick.AddListener(HandleSubmit);
            inputField.onValueChanged.AddListener(HandleInputChanged);

            if (submitInput != null && submitOnInput)
                submitInput.OnStart += HandleSubmit;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            submitButton.onClick.RemoveListener(HandleSubmit);
            inputField.onValueChanged.RemoveListener(HandleInputChanged);

            if (submitInput != null && submitOnInput)
                submitInput.OnStart -= HandleSubmit;
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);

            var state = new GameState() {
                VariableName = variableName,
                SummaryText = summaryText.text,
                InputFieldText = inputField.text,
                PlayOnSubmit = playOnSubmit
            };
            stateMap.SetState(state);
        }

        protected override async UniTask DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            var state = stateMap.GetState<GameState>();
            if (state is null) return;

            variableName = state.VariableName;
            summaryText.text = state.SummaryText;
            summaryText.gameObject.SetActive(!string.IsNullOrWhiteSpace(state.SummaryText));
            inputField.text = state.InputFieldText;
            playOnSubmit = state.PlayOnSubmit;
        }

        protected virtual void HandleInputChanged (string text)
        {
            submitButton.interactable = !string.IsNullOrWhiteSpace(text);
        }

        protected virtual void HandleSubmit ()
        {
            if (!Visible || string.IsNullOrWhiteSpace(inputField.text)) return;

            stateManager.PeekRollbackStack()?.AllowPlayerRollback();

            variableManager.SetVariableValue(variableName, inputField.text);

            ClearFocus();
            Hide();

            if (playOnSubmit)
            {
                // Attempt to select and play next command.
                var nextIndex = scriptPlayer.PlayedIndex + 1;
                scriptPlayer.Play(scriptPlayer.Playlist, nextIndex);
            }
        }
    }
}
