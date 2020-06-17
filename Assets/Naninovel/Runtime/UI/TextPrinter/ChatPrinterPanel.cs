// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <summary>
    /// A <see cref="UITextPrinterPanel"/> implementation for a chat-style printer.
    /// </summary>
    public class ChatPrinterPanel : UITextPrinterPanel
    {
        [System.Serializable]
        public class GameState
        {
            public List<ChatMessage.State> Messages;
            public string LastMesssageText;
        }

        public override string PrintedText { get => printedText; set => SetPrintedText(value); }
        public override string AuthorNameText { get; set; }
        public override float RevealProgress 
        { 
            get => revealProgress; 
            set 
            { 
                if (value == 0) DestroyAllMessages(); 
                else if (messageStack?.Count > 0 && messageStack.Peek() is ChatMessage message && message) 
                    message.MessageText = lastMesssageText;
            } 
        }
        public override string Apperance { get; set; }

        protected ScrollRect ScrollRect => scrollRect;
        protected RectTransform MessagesContainer => messagesContainer;
        protected ChatMessage MessagePrototype => messagePrototype;
        protected ScriptableUIBehaviour InputIndicator => inputIndicator;
        protected float RevealDelayModifier => revealDelayModifier;
        protected float PrintDotDelay => printDotDelay;

        [SerializeField] private ScrollRect scrollRect = default;
        [SerializeField] private RectTransform messagesContainer = default;
        [SerializeField] private ChatMessage messagePrototype = default;
        [SerializeField] private ScriptableUIBehaviour inputIndicator = default;
        [SerializeField] private float revealDelayModifier = 3f;
        [SerializeField] private float printDotDelay = .5f;

        private Stack<ChatMessage> messageStack = new Stack<ChatMessage>();
        private ICharacterManager characterManager;
        private IStateManager stateManager;
        private string lastAuthorId;
        private string printedText;
        private string lastMesssageText;
        private float revealProgress = .1f;

        public override async UniTask RevealPrintedTextOverTimeAsync (float revealDelay, CancellationToken cancellationToken)
        {
            var message = AddMessage(string.Empty, lastAuthorId);

            revealProgress = .1f;

            if (revealDelay > 0 && lastMesssageText != null)
            {
                await AsyncUtils.WaitEndOfFrame;
                if (cancellationToken.IsCancellationRequested) return;
                ScrollToBottom(); // Wait before scrolling, otherwise it's not scrolled.

                var revealDuration = lastMesssageText.Count(c => char.IsLetterOrDigit(c)) * revealDelay * revealDelayModifier;
                var revealStartTime = Time.time;
                var revealFinishTime = revealStartTime + revealDuration;
                var lastPrintDotTime = 0f;
                while (revealFinishTime > Time.time && messageStack.Count > 0 && messageStack.Peek() == message)
                {
                    // Print dots while waiting.
                    if (Time.time >= lastPrintDotTime + printDotDelay)
                    {
                        lastPrintDotTime = Time.time;
                        message.MessageText = message.MessageText.Length >= 9 ? string.Empty : message.MessageText + " . ";
                    }

                    revealProgress = (Time.time - revealStartTime) / revealDuration;

                    await AsyncUtils.WaitEndOfFrame;
                    if (cancellationToken.IsCancellationRequested) return;
                }
            }

            if (messageStack.Contains(message))
                message.MessageText = lastMesssageText;

            ScrollToBottom();

            revealProgress = 1f;
        }

        public override void SetWaitForInputIndicatorVisible (bool isVisible)
        {
            if (isVisible) inputIndicator.Show();
            else inputIndicator.Hide();
        }

        public override void OnAuthorChanged (string authorId, CharacterMetadata authorMeta)
        {
            lastAuthorId = authorId;
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(scrollRect, messagesContainer, messagePrototype, inputIndicator);

            characterManager = Engine.GetService<ICharacterManager>();
            stateManager = Engine.GetService<IStateManager>();
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

            stateManager.RemoveOnGameSerializeTask(SerializeState);
            stateManager.RemoveOnGameDeserializeTask(DeserializeState);
        }

        protected virtual void SetPrintedText (string value)
        {
            printedText = value;

            if (messageStack.Count == 0 || string.IsNullOrEmpty(lastMesssageText))
                lastMesssageText = value;
            else
            {
                var previousText = string.Join(string.Empty, messageStack.Select(m => m.MessageText).Reverse());
                lastMesssageText = value.GetAfterFirst(previousText);
            }
        }

        protected virtual ChatMessage AddMessage (string messageText, string authorId = null, bool instant = false)
        {
            var message = Instantiate(messagePrototype);
            message.transform.SetParent(messagesContainer, false);
            message.MessageText = messageText;
            message.AuthorId = authorId;

            if (!string.IsNullOrEmpty(authorId))
            {
                message.ActorNameText = characterManager.GetDisplayName(authorId);
                message.AvatarTexture = CharacterManager.GetAvatarTextureFor(authorId);

                var meta = characterManager.Configuration.GetMetadataOrDefault(authorId);
                if (meta.UseCharacterColor)
                {
                    message.MessageColor = meta.MessageColor;
                    message.ActorNameTextColor = meta.NameColor;
                }
            }
            else
            {
                message.ActorNameText = string.Empty;
                message.AvatarTexture = null;
            }

            if (instant) message.Visible = true;
            else message.Show();

            messageStack.Push(message);
            return message;
        }

        protected virtual void DestroyAllMessages ()
        {
            while (messageStack.Count > 0)
            {
                var message = messageStack.Pop();
                ObjectUtils.DestroyOrImmediate(message.gameObject);
            }
        }

        private async void ScrollToBottom ()
        {
            // Wait a frame and force rebuild layout before setting scroll position,
            // otherwise it's ignoring recently added messages.
            await AsyncUtils.WaitEndOfFrame;
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 0;
        }

        private void SerializeState (GameStateMap stateMap)
        {
            var state = new GameState() {
                Messages = messageStack.Select(m => m.GetState()).Reverse().ToList(),
                LastMesssageText = lastMesssageText
            };
            stateMap.SetState(state);
        }

        private UniTask DeserializeState (GameStateMap stateMap)
        {
            DestroyAllMessages();
            lastMesssageText = null;

            var state = stateMap.GetState<GameState>();
            if (state is null) return UniTask.CompletedTask;

            if (state.Messages?.Count > 0)
                foreach (var message in state.Messages)
                    AddMessage(message.PrintedText, message.AuthorId, true);

            lastMesssageText = state.LastMesssageText;

            ScrollToBottom();

            return UniTask.CompletedTask;
        }
    } 
}
