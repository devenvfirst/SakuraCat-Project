// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class ConfirmationPanel : CustomUI, IConfirmationUI
    {
        protected Text MessageText => messageText;
        protected ScriptableLabeledButton ConfirmButton => confirmButton;
        protected ScriptableLabeledButton CancelButton => cancelButton;
        protected ScriptableLabeledButton CloseButton => closeButton;

        [SerializeField] private Text messageText = default;
        [SerializeField] private ScriptableLabeledButton confirmButton = default;
        [SerializeField] private ScriptableLabeledButton cancelButton = default;
        [SerializeField] private ScriptableLabeledButton closeButton = default;

        private IInputManager inputManager;
        private bool? userConfirmed;

        public virtual async UniTask<bool> ConfirmAsync (string message)
        {
            if (Visible) return false;

            closeButton.gameObject.SetActive(false);
            confirmButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);

            messageText.text = message;

            Show();

            while (!userConfirmed.HasValue)
                await AsyncUtils.WaitEndOfFrame;

            var result = userConfirmed.Value;
            userConfirmed = null;

            Hide();

            return result;
        }

        public virtual async UniTask NotifyAsync (string message)
        {
            if (Visible) return;

            closeButton.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(false);

            messageText.text = message;

            Show();

            while (!userConfirmed.HasValue)
                await AsyncUtils.WaitEndOfFrame;

            userConfirmed = null;

            Hide();
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(messageText, confirmButton, cancelButton, closeButton);

            inputManager = Engine.GetService<IInputManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            inputManager.AddBlockingUI(this);
            confirmButton.OnButtonClicked += Confirm;
            cancelButton.OnButtonClicked += Cancel;
            closeButton.OnButtonClicked += Confirm;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            inputManager?.RemoveBlockingUI(this);
            confirmButton.OnButtonClicked -= Confirm;
            cancelButton.OnButtonClicked -= Cancel;
            closeButton.OnButtonClicked -= Confirm;
        }

        private void Confirm ()
        {
            if (!Visible) return;
            userConfirmed = true;
        }

        private void Cancel ()
        {
            if (!Visible) return;
            userConfirmed = false;
        }
    }
}
