// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class BacklogPanel : CustomUI, IBacklogUI
    {
        [System.Serializable]
        public new class GameState
        {
            public List<BacklogMessage.State> Messages;
        }

        protected virtual BacklogMessage LastMessage => messages.LastOrDefault();
        protected RectTransform MessagesContainer => messagesContainer;
        protected ScrollRect ScrollRect => scrollRect;
        protected BacklogMessage MessagePrefab => messagePrefab;
        protected bool ClearOnLoad => clearOnLoad;
        protected bool ClearOnLocaleChange => clearOnLocaleChange;
        protected int Capacity => Capacity;
        protected int SaveCapacity => SaveCapacity;
        protected bool StripTags => stripTags;
        protected bool AddChoices => addChoices;

        [SerializeField] private RectTransform messagesContainer = default;
        [SerializeField] private ScrollRect scrollRect = default;
        [SerializeField] private BacklogMessage messagePrefab = default;
        [Tooltip("Whether to clear the backlog when loading another script or resetting state (exiting to title).")]
        [SerializeField] private bool clearOnLoad = true;
        [Tooltip("Whether to clear the backlog when changing game localization (language).")]
        [SerializeField] private bool clearOnLocaleChange = true;
        [Tooltip("How many messages should the backlog keep.")]
        [SerializeField] private int capacity = 100;
        [Tooltip("How many messages should the backlog keep when saving the game.")]
        [SerializeField] private int saveCapacity = 30;
        [Tooltip("Whether to strip formatting content (content inside `<` `>` and the angle brackets themselves) from the added messages.")]
        [SerializeField] private bool stripTags = true;
        [Tooltip("Whether to add choices summary to the log.")]
        [SerializeField] private bool addChoices = true;
        [Tooltip("Template to use for selected choice summary. " + choiceTemplateLiteral + " will be replaced with the actual choice summary.")]
        [SerializeField] private string selectedChoiceTemplate = $"    <b>{choiceTemplateLiteral}</b>";
        [Tooltip("Template to use for other (not selected) choice summary. " + choiceTemplateLiteral + " will be replaced with the actual choice summary.")]
        [SerializeField] private string otherChoiceTemplate = $"    <color=#ffffff88>{choiceTemplateLiteral}</color>";
        [Tooltip("String added between consequent choices.")]
        [SerializeField] private string choiceSeparator = "<br>";

        private const int messageLengthLimit = 10000; // Due to Unity's mesh verts count limit.
        private const string choiceTemplateLiteral = "%SUMMARY%";

        /// <summary>
        /// Capture formatting content enclosed in `<>`.
        /// </summary>
        private static readonly Regex formattingRegex = new Regex(@"<.*?>");

        private readonly List<BacklogMessage> messages = new List<BacklogMessage>();
        private readonly Stack<BacklogMessage> messagesPool = new Stack<BacklogMessage>();
        private IInputManager inputManager;
        private ICharacterManager charManager;
        private IStateManager stateManager;
        private ILocalizationManager localizationManager;
        private bool clearPending;

        public virtual void Clear ()
        {
            foreach (var message in messages)
            {
                message.gameObject.SetActive(false);
                messagesPool.Push(message);
            }
            messages.Clear();
        }

        public virtual void AddMessage (string messageText, string actorId = null, string voiceClipName = null)
        {
            if (StripTags) messageText = StripFormatting(messageText);

            var actorNameText = charManager.GetDisplayName(actorId) ?? actorId;
            SpawnMessage(messageText, actorNameText, voiceClipName != null ? new List<string> { voiceClipName } : null);
        }

        public virtual void AppendMessage (string message, string voiceClipName = null)
        {
            if (!LastMessage) return;

            if (StripTags) message = StripFormatting(message);

            if ((LastMessage.Message.Length + message.Length) > messageLengthLimit)
            {
                SpawnMessage(message, LastMessage.ActorName, voiceClipName != null ? new List<string> { voiceClipName } : null);
                return;
            }

            LastMessage.AppendText(message);
            if (!string.IsNullOrWhiteSpace(voiceClipName))
                LastMessage.AddVoiceClipName(voiceClipName);
        }

        public virtual void AddChoice (List<Tuple<string, bool>> summary)
        {
            if (!AddChoices || summary.All(t => string.IsNullOrWhiteSpace(t.Item1))) return;

            var messages = summary.Select(t => t.Item2 ? selectedChoiceTemplate.Replace(choiceTemplateLiteral, t.Item1) : otherChoiceTemplate.Replace(choiceTemplateLiteral, t.Item1));
            var separator = choiceSeparator.Replace("<br>", Environment.NewLine);
            var messageText = string.Join(separator, messages);

            SpawnMessage(messageText, null);
        }

        public override void SetVisibility (bool visible)
        {
            if (visible) ScrollToBottom();
            base.SetVisibility(visible);
        }

        public override UniTask ChangeVisibilityAsync (bool visible, float? duration = null)
        {
            if (visible) ScrollToBottom();
            return base.ChangeVisibilityAsync(visible, duration);
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(messagesContainer, scrollRect, messagePrefab);

            inputManager = Engine.GetService<IInputManager>();
            charManager = Engine.GetService<ICharacterManager>();
            stateManager = Engine.GetService<IStateManager>();
            localizationManager = Engine.GetService<ILocalizationManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (inputManager?.GetShowBacklog() != null)
                inputManager.GetShowBacklog().OnStart += Show;

            if (clearOnLoad)
            {
                stateManager.OnGameLoadStarted += Clear;
                stateManager.OnResetStarted += Clear;
            }

            if (clearOnLocaleChange)
            {
                localizationManager.OnLocaleChanged += SetClearPending;
                stateManager.OnGameLoadFinished += ClearIfPending;
            }
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (inputManager?.GetShowBacklog() != null)
                inputManager.GetShowBacklog().OnStart -= Show;

            if (stateManager != null)
            {
                stateManager.OnGameLoadStarted -= Clear;
                stateManager.OnGameLoadFinished -= ClearIfPending;
                stateManager.OnResetStarted -= Clear;
            }

            if (localizationManager != null)
                localizationManager.OnLocaleChanged -= SetClearPending;
        }

        protected virtual void SpawnMessage (string messageText, string actorNameText, List<string> voiceClipNames = null)
        {
            var message = default(BacklogMessage);

            if (messages.Count > capacity)
            {
                message = messages.First();
                message.transform.SetSiblingIndex(messagesContainer.transform.childCount - 1);
            }
            else
            {
                if (messagesPool.Count > 0)
                {
                    message = messagesPool.Pop();
                    message.gameObject.SetActive(true);
                    message.transform.SetSiblingIndex(messagesContainer.transform.childCount - 1);
                }
                else
                {
                    message = Instantiate(messagePrefab);
                    message.transform.SetParent(messagesContainer.transform, false);
                }

                messages.Add(message);
            }

            message.Initialize(messageText, actorNameText, voiceClipNames);
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            messagesContainer.gameObject.SetActive(visible);
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);
            var state = new GameState() {
                Messages = messages.Take(saveCapacity).Select(m => m.GetState()).ToList()
            };
            stateMap.SetState(state);
        }

        protected override async UniTask DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            Clear();

            var state = stateMap.GetState<GameState>();
            if (state is null) return;

            if (state.Messages?.Count > 0)
                foreach (var messageState in state.Messages)
                    SpawnMessage(messageState.MessageText, messageState.ActorNameText, messageState.VoiceClipNames);
        }

        protected virtual string StripFormatting (string content) => formattingRegex.Replace(content, string.Empty);

        private async void ScrollToBottom ()
        {
            // Wait a frame and force rebuild layout before setting scroll position,
            // otherwise it's ignoring recently added messages.
            await AsyncUtils.WaitEndOfFrame;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 0;
        }

        private void SetClearPending (string locale) => clearPending = true;

        private void Clear (GameSaveLoadArgs args) => Clear();

        private void ClearIfPending (GameSaveLoadArgs args)
        {
            if (!clearPending) return;
            clearPending = false;
            Clear();
        }
    }
}
