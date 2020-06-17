// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Used by <see cref="UITextPrinter"/> to control the printed text.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UITextPrinterPanel : ScriptableUIBehaviour, IManagedUI
    {
        /// <summary>
        /// Contents of the printer to be used for transformations.
        /// </summary>
        public RectTransform Content => content;
        /// <summary>
        /// The text to be printed inside the printer panel. 
        /// Note that the visibility of the text is controlled independently.
        /// </summary>
        public abstract string PrintedText { get; set; }
        /// <summary>
        /// Text representing name of the author of the currently printed text.
        /// </summary>
        public abstract string AuthorNameText { get; set; }
        /// <summary>
        /// Which part of the assigned text message is currently revealed, in 0.0 to 1.0 range.
        /// </summary>
        public abstract float RevealProgress { get; set; }
        /// <summary>
        /// Current appearance of the printer.
        /// </summary>
        public abstract string Apperance { get; set; }
        /// <summary>
        /// Object that should trigger continue input when interacted with.
        /// </summary>
        public GameObject ContinueInputTrigger => continueInputTrigger;

        protected ICharacterManager CharacterManager { get; private set; }

        [Tooltip("Transform used for printer position, scale and rotation external manipulations.")]
        [SerializeField] private RectTransform content = default;
        [Tooltip("Object that should trigger continue input when interacted with. Make sure the object is a raycast target and is not blocked by other raycast target objects.")]
        [SerializeField] private GameObject continueInputTrigger = default;

        private IInputManager inputManager;
        private IScriptPlayer scriptPlayer;

        public virtual UniTask InitializeAsync ()
        {
            inputManager?.GetContinue()?.AddObjectTrigger(ContinueInputTrigger);
            scriptPlayer.OnWaitingForInput += SetWaitForInputIndicatorVisible;
            return UniTask.CompletedTask;
        }

        UniTask IManagedUI.ChangeVisibilityAsync (bool visible, float? duration)
        {
            Debug.LogError("@showUI and @hideUI commands can't be used with text printers; use @show/hide or @show/hidePrinter commands instead");
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Reveals the <see cref="PrintedText"/> char by char over time.
        /// </summary>
        /// <param name="revealDelay">Delay (in seconds) between revealing consequent characters.</param>
        /// <param name="cancellationToken">The reveal should be canceled when requested by the provided token.</param>
        public abstract UniTask RevealPrintedTextOverTimeAsync (float revealDelay, CancellationToken cancellationToken);
        /// <summary>
        /// Controls visibility of the wait for input indicator.
        /// </summary>
        public abstract void SetWaitForInputIndicatorVisible (bool isVisible);
        /// <summary>
        /// Invoked by <see cref="UITextPrinter"/> when author meta of the printed text changes.
        /// </summary>
        /// <param name="authorId">Acotr ID of the new author.</param>
        /// <param name="authorMeta">Metadata of the new author.</param>
        public abstract void OnAuthorChanged (string authorId, CharacterMetadata authorMeta);

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(content, continueInputTrigger);

            inputManager = Engine.GetService<IInputManager>();
            scriptPlayer = Engine.GetService<IScriptPlayer>();

            CharacterManager = Engine.GetService<ICharacterManager>();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            inputManager?.GetContinue()?.RemoveObjectTrigger(ContinueInputTrigger);
            if (scriptPlayer != null)
                scriptPlayer.OnWaitingForInput -= SetWaitForInputIndicatorVisible;
        }
    } 
}
