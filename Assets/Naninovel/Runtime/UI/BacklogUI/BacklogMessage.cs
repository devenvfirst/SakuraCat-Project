// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel
{
    public class BacklogMessage : ScriptableUIBehaviour
    {
        [System.Serializable]
        public struct State
        {
            public string MessageText;
            public string ActorNameText;
            public List<string> VoiceClipNames;
        }

        public virtual string Message => messageText.text;
        public virtual string ActorName => actorNameText.text;

        protected Text MessageText => messageText;
        protected Text ActorNameText => actorNameText;
        protected Button PlayVoiceButton => playVoiceButton;

        [SerializeField] private Text messageText = default;
        [SerializeField] private Text actorNameText = default;
        [SerializeField] private Button playVoiceButton = default;

        private readonly List<string> voiceClipNames = new List<string>();
        private IAudioManager audioManager;

        public virtual State GetState () => new State { 
            MessageText = messageText.text, 
            ActorNameText = actorNameText.text, 
            VoiceClipNames = voiceClipNames 
        };

        public virtual void Initialize (string message, string actorName, List<string> voiceClipNames = null)
        {
            messageText.text = message;
            if (string.IsNullOrWhiteSpace(actorName))
            {
                actorNameText.text = null;
                actorNameText.gameObject.SetActive(false);
            }
            else
            {
                actorNameText.text = actorName;
                actorNameText.gameObject.SetActive(true);
            }

            if (voiceClipNames != null)
                foreach (var clipName in voiceClipNames)
                    AddVoiceClipName(clipName);
            else
            {
                this.voiceClipNames.Clear();
                playVoiceButton.gameObject.SetActive(false);
            }
        }

        public virtual void AppendText (string text)
        {
            messageText.text += text;
        }

        public virtual async void AddVoiceClipName (string voiceClipName)
        {
            if (string.IsNullOrWhiteSpace(voiceClipName)) return;
            if (!await audioManager.VoiceExistsAsync(voiceClipName)) return;

            voiceClipNames.Add(voiceClipName);
            playVoiceButton.gameObject.SetActive(true);
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(messageText, actorNameText, playVoiceButton);
            audioManager = Engine.GetService<IAudioManager>();
            actorNameText.text = null;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();
            playVoiceButton.onClick.AddListener(HandlePlayVoiceButtonClicked);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();
            playVoiceButton.onClick.RemoveListener(HandlePlayVoiceButtonClicked);
        }

        protected virtual async void HandlePlayVoiceButtonClicked ()
        {
            playVoiceButton.interactable = false;
            await audioManager.PlayVoiceSequenceAsync(voiceClipNames);
            playVoiceButton.interactable = true;
        }
    }
}
