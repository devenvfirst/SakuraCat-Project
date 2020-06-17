// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.UI;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    public class GameSettingsPreviewPrinter : ScriptableUIBehaviour
    {
        protected RevealableUIText RevealableText => revealableText;

        [SerializeField] private RevealableUIText revealableText = default;

        private CancellationTokenSource revealCts;
        private ITextPrinterManager printerManager;

        public override void Show ()
        {
            base.Show();

            StartPrinting();
        }

        public virtual void StartPrinting ()
        {
            if (revealCts != null)
                revealCts.Cancel();

            revealableText.RevealProgress = 0;
            revealableText.Rebuild(UnityEngine.UI.CanvasUpdate.PreRender); // Otherwise it's not displaying anything.

            var revealDelay = Mathf.Lerp(printerManager.Configuration.MaxRevealDelay, 0, printerManager.BaseRevealSpeed);
            if (revealDelay == 0)
                revealableText.RevealProgress = 1;
            else
            {
                revealCts = new CancellationTokenSource();
                RevealTextOverTimeAsync(revealDelay, revealCts.Token).Forget();
            }
        }

        protected override void Awake ()
        {
            base.Awake();

            this.AssertRequiredObjects(revealableText);
            printerManager = Engine.GetService<ITextPrinterManager>();
        }

        protected virtual async UniTask RevealTextOverTimeAsync (float revealDelay, CancellationToken cancellationToken)
        {
            var lastRevealTime = Time.time;
            while (revealableText.RevealProgress < 1)
            {
                var timeSinceLastReveal = Time.time - lastRevealTime;
                var charsToReveal = Mathf.FloorToInt(timeSinceLastReveal / revealDelay);
                if (charsToReveal > 0)
                {
                    lastRevealTime = Time.time;
                    revealableText.RevealNextChars(charsToReveal, revealDelay, cancellationToken);
                    while (revealableText.Revealing && !cancellationToken.IsCancellationRequested)
                        await AsyncUtils.WaitEndOfFrame;
                    if (cancellationToken.IsCancellationRequested) return;
                }
                await AsyncUtils.WaitEndOfFrame;
            }

            var autoPlayDelay = Mathf.Lerp(0, printerManager.Configuration.MaxAutoWaitDelay, printerManager.BaseAutoDelay) * revealableText.Text.Count(char.IsLetterOrDigit);
            var waitUntilTime = Time.time + autoPlayDelay;
            while (Time.time < waitUntilTime && !cancellationToken.IsCancellationRequested)
                await AsyncUtils.WaitEndOfFrame;

            if (cancellationToken.IsCancellationRequested) return;

            StartPrinting();
        }
    }
}