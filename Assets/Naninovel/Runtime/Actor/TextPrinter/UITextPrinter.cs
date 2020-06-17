// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.UI;
using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ITextPrinterActor"/> implementation using <see cref="UITextPrinterPanel"/> to represent the actor.
    /// </summary>
    public class UITextPrinter : MonoBehaviourActor, ITextPrinterActor
    {
        public override string Appearance { get => PrinterPanel.Apperance; set => PrinterPanel.Apperance = value; }
        public override bool Visible { get => PrinterPanel.Visible; set => PrinterPanel.Visible = value; }
        public string Text { get => text; set => SetText(value); }
        public string AuthorId { get => authorId; set => SetAuthorId(value); }
        public List<string> RichTextTags { get => richTextTags; set => SetRichTextTags(value); }
        public float RevealProgress { get => PrinterPanel.RevealProgress; set { CancelRevealTextRoutine(); PrinterPanel.RevealProgress = value; } }

        protected UITextPrinterPanel PrinterPanel { get; private set; }
        protected bool UsingRichTags => richTextTags.Count > 0;

        private readonly List<string> richTextTags = new List<string>();
        private readonly IUIManager uiManager;
        private readonly ICharacterManager characterManager;
        private readonly TextPrinterMetadata metadata;
        private string text, authorId;
        private CancellationTokenSource revealTextCTS;
        private string activeOpenTags, activeCloseTags;

        public UITextPrinter (string id, TextPrinterMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;
            uiManager = Engine.GetService<IUIManager>();
            characterManager = Engine.GetService<ICharacterManager>();
            activeOpenTags = string.Empty;
            activeCloseTags = string.Empty;
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerMngr = Engine.GetService<IResourceProviderManager>();
            var prefabResource = await metadata.Loader.CreateFor<GameObject>(providerMngr).LoadAsync(Id);
            if (!prefabResource.IsValid)
            {
                Debug.LogError($"Failed to load `{Id}` UI text printer resource object. Make sure the printer is correctly configured.");
                return;
            }

            PrinterPanel = await uiManager.InstantiatePrefabAsync(prefabResource.Object) as UITextPrinterPanel;
            PrinterPanel.transform.SetParent(Transform);
            PrinterPanel.PrintedText = string.Empty;
            RevealProgress = 0f;

            SetAuthorId(null);
            Visible = false;
        }

        public override UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            Appearance = appearance;
            return UniTask.CompletedTask;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            await PrinterPanel.ChangeVisibilityAsync(visible, duration);
        }

        public virtual async UniTask RevealTextAsync (float revealDelay, CancellationToken cancellationToken = default)
        {
            CancelRevealTextRoutine();

            revealTextCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await PrinterPanel.RevealPrintedTextOverTimeAsync(revealDelay, revealTextCTS.Token);
        }

        public void SetRichTextTags (List<string> tags)
        {
            richTextTags.Clear();

            if (tags?.Count > 0)
                richTextTags.AddRange(tags);

            if (UsingRichTags)
            {
                activeOpenTags = GetActiveTagsOpenSequence();
                activeCloseTags = GetActiveTagsCloseSequence();
            }
            else
            {
                activeOpenTags = string.Empty;
                activeCloseTags = string.Empty;
            }

            SetText(Text); // Update the printed text with the tags.
        }

        public void SetAuthorId (string authorId)
        {
            this.authorId = authorId;

            // Attempt to find a character display name for the provided actor ID.
            var displayName = characterManager.GetDisplayName(authorId) ?? authorId;
            PrinterPanel.AuthorNameText = displayName;

            // Update author meta.
            var authorMeta = characterManager.Configuration.GetMetadataOrDefault(authorId);
            PrinterPanel.OnAuthorChanged(authorId, authorMeta);
        }

        public override void Dispose ()
        {
            base.Dispose();

            CancelRevealTextRoutine();

            if (PrinterPanel != null)
            {
                ObjectUtils.DestroyOrImmediate(PrinterPanel.gameObject);
                PrinterPanel = null;
            }
        }

        protected override Vector3 GetBehaviourPosition ()
        {
            // Changing transform of the root obj won't work; modify content panel instead.
            if (!PrinterPanel || !PrinterPanel.Content) return Vector3.zero;

            // Printer position is always relative (0.0-1.0) to parent rect.
            var scenePos = new Vector3(
                PrinterPanel.Content.anchoredPosition.x / PrinterPanel.RectTransform.rect.width,
                PrinterPanel.Content.anchoredPosition.y / PrinterPanel.RectTransform.rect.height,
                PrinterPanel.Content.position.z);

            return scenePos;
        }

        protected override void SetBehaviourPosition (Vector3 position)
        {
            // Changing transform of the root obj won't work; modify content panel instead.
            if (!PrinterPanel || !PrinterPanel.Content) return;

            // Printer position is always relative (0.0-1.0) to parent rect.
            var anchordPos = new Vector3(
                position.x * PrinterPanel.RectTransform.rect.width,
                position.y * PrinterPanel.RectTransform.rect.height,
                PrinterPanel.Content.position.z);

            PrinterPanel.Content.anchoredPosition = anchordPos;
        }

        protected override Quaternion GetBehaviourRotation ()
        {
            if (!PrinterPanel || !PrinterPanel.Content) return Quaternion.identity;
            return PrinterPanel.Content.rotation;
        }

        protected override void SetBehaviourRotation (Quaternion rotation)
        {
            if (!PrinterPanel || !PrinterPanel.Content) return;
            PrinterPanel.Content.rotation = rotation;
        }

        protected override Vector3 GetBehaviourScale ()
        {
            if (!PrinterPanel || !PrinterPanel.Content) return Vector3.one;
            return PrinterPanel.Content.localScale;
        }

        protected override void SetBehaviourScale (Vector3 scale)
        {
            if (!PrinterPanel || !PrinterPanel.Content) return;
            PrinterPanel.Content.localScale = scale;
        }

        protected override Color GetBehaviourTintColor () => Color.white;

        protected override void SetBehaviourTintColor (Color tintColor) { }

        private void SetText (string value)
        {
            if (value is null)
                value = string.Empty;

            text = value;

            // Handle rich text tags before assigning the actual text.
            PrinterPanel.PrintedText = UsingRichTags ? string.Concat(activeOpenTags, value, activeCloseTags) : value;
        }

        private void CancelRevealTextRoutine ()
        {
            revealTextCTS?.Cancel();
            revealTextCTS?.Dispose();
            revealTextCTS = null;
        }

        private string GetActiveTagsOpenSequence ()
        {
            var result = string.Empty;

            if (RichTextTags is null || RichTextTags.Count == 0)
                return result;

            foreach (var tag in RichTextTags)
                result += string.Format("<{0}>", tag);

            return result;
        }

        private string GetActiveTagsCloseSequence ()
        {
            var result = string.Empty;

            if (RichTextTags is null || RichTextTags.Count == 0)
                return result;

            var reversedActiveTags = RichTextTags;
            reversedActiveTags.Reverse();
            foreach (var tag in reversedActiveTags)
                result += string.Format("</{0}>", tag.GetBefore("=") ?? tag);

            return result;
        }
    } 
}
