// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Prints (reveals over time) specified text message using a text printer actor.
    /// </summary>
    /// <remarks>
    /// This command is used under the hood when processing generic text lines, eg generic line `Kohaku: Hello World!` will be 
    /// automatically tranformed into `@print "Hello World!" author:Kohaku` when parsing the naninovel scripts.<br/>
    /// Will reset (clear) the printer before printing the new message by default; set `reset` parameter to *false* or disable `Auto Reset` in the printer actor configuration to prevent that and append the text instead.<br/>
    /// Will make the printer default and hide other printers by default; set `default` parameter to *false* or disable `Auto Default` in the printer actor configuration to prevent that.<br/>
    /// Will wait for user input before finishing the task by default; set `waitInput` parameter to *false* or disable `Auto Wait` in the printer actor configuration to return as soon as the text is fully revealed.<br/>
    /// </remarks>
    /// <example>
    /// ; Will print the phrase with a default printer.
    /// @print "Lorem ipsum dolor sit amet."
    /// 
    /// ; To include quotes in the text itself, escape them.
    /// @print "Saying \"Stop the car\" was a mistake."
    /// 
    /// ; Reveal message with half of the normal speed and 
    /// ; don't wait for user input to continue.
    /// @print "Lorem ipsum dolor sit amet." speed:0.5 waitInput:false
    /// </example>
    [CommandAlias("print")]
    public class PrintText : PrinterCommand, Command.IPreloadable, Command.ILocalizable
    {
        /// <summary>
        /// Text of the message to print.
        /// When the text contain spaces, wrap it in double quotes (`"`). 
        /// In case you wish to include the double quotes in the text itself, escape them.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter Text;
        /// <summary>
        /// ID of the printer actor to use. Will use a default one when not provided.
        /// </summary>
        [ParameterAlias("printer")]
        public StringParameter PrinterId;
        /// <summary>
        /// ID of the actor, which should be associated with the printed message.
        /// </summary>
        [ParameterAlias("author")]
        public StringParameter AuthorId;
        /// <summary>
        /// Text reveal speed multiplier; should be positive or zero. Setting to one will yield the default speed.
        /// </summary>
        [ParameterAlias("speed")]
        public DecimalParameter RevealSpeed = 1f;
        /// <summary>
        /// Whether to reset text of the printer before executing the printing task.
        /// Default value is controlled via `Auto Reset` property in the printer actor configuration menu.
        /// </summary>
        [ParameterAlias("reset")]
        public BooleanParameter ResetPrinter;
        /// <summary>
        /// Whether to make the printer default and hide other printers before executing the printing task.
        /// Default value is controlled via `Auto Default` property in the printer actor configuration menu.
        /// </summary>
        [ParameterAlias("default")]
        public BooleanParameter DefaultPrinter;
        /// <summary>
        /// Whether to wait for user input after finishing the printing task.
        /// Default value is controlled via `Auto Wait` property in the printer actor configuration menu.
        /// </summary>
        [ParameterAlias("waitInput")]
        public BooleanParameter WaitForInput;
        /// <summary>
        /// Number of line breaks to prepend before the printed text.
        /// Default value is controlled via `Auto Line Break` property in the printer actor configuration menu.
        /// </summary>
        [ParameterAlias("br")]
        public IntegerParameter LineBreaks;
        /// <summary>
        /// Controls duration (in seconds) of the printers show and hide animations associated with this command.
        /// Default value for each printer is set in the actor configuration.
        /// </summary>
        [ParameterAlias("fadeTime")]
        public DecimalParameter ChangeVisibilityDuration;

        protected override string AssignedPrinterId => PrinterId;
        protected override string AssignedAuthorId => AuthorId;
        protected virtual string AutoVoicePath => AudioConfiguration.GetAutoVoiceClipPath(PlaybackSpot);
        protected virtual bool PlayAutoVoice => !string.IsNullOrEmpty(PlaybackSpot.ScriptName) && AudioManager.Configuration.EnableAutoVoicing;
        protected IAudioManager AudioManager => Engine.GetService<IAudioManager>();
        protected IInputManager InputManager => Engine.GetService<IInputManager>();
        protected IScriptPlayer ScriptPlayer => Engine.GetService<IScriptPlayer>();
        protected CharacterMetadata AuthorMeta => Engine.GetService<ICharacterManager>().Configuration.GetMetadataOrDefault(AuthorId);

        public override async UniTask HoldResourcesAsync ()
        {
            await base.HoldResourcesAsync();

            if (PlayAutoVoice)
                await AudioManager.HoldVoiceResourcesAsync(this, AutoVoicePath);

            if (Assigned(AuthorId) && !AuthorId.DynamicValue && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                await AudioManager.HoldAudioResourcesAsync(this, AuthorMeta.MessageSound);
        }

        public override void ReleaseResources ()
        {
            base.ReleaseResources();

            if (PlayAutoVoice)
                AudioManager.ReleaseVoiceResources(this, AutoVoicePath);

            if (Assigned(AuthorId) && !AuthorId.DynamicValue && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                AudioManager.ReleaseAudioResources(this, AuthorMeta.MessageSound);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var printer = await GetOrAddPrinterAsync();
            if (cancellationToken.IsCancellationRequested) return;

            var printerMeta = PrinterManager.Configuration.GetMetadataOrDefault(printer.Id);

            if (!printer.Visible)
            {
                var showDuration = Assigned(ChangeVisibilityDuration) ? ChangeVisibilityDuration.Value : printerMeta.ChangeVisibilityDuration;
                printer.ChangeVisibilityAsync(true, showDuration).Forget();
            }

            if ((!Assigned(DefaultPrinter) && printerMeta.AutoDefault) || (Assigned(DefaultPrinter) && DefaultPrinter.Value))
            {
                if (PrinterManager.DefaultPrinterId != printer.Id)
                    PrinterManager.DefaultPrinterId = printer.Id;
                foreach (var otherPrinter in PrinterManager.GetAllActors())
                    if (otherPrinter.Id != printer.Id && otherPrinter.Visible)
                    {
                        var otherPrinterMeta = PrinterManager.Configuration.GetMetadataOrDefault(otherPrinter.Id);
                        var otherPrinterHideDuration = Assigned(ChangeVisibilityDuration) ? ChangeVisibilityDuration.Value : otherPrinterMeta.ChangeVisibilityDuration;
                        otherPrinter.ChangeVisibilityAsync(false, otherPrinterHideDuration).Forget();
                    }
            }

            if (PlayAutoVoice)
            {
                if (AudioManager.Configuration.VoiceOverlapPolicy == VoiceOverlapPolicy.PreventCharacterOverlap && printer.AuthorId == AuthorId && !string.IsNullOrEmpty(AudioManager.GetPlayedVoicePath()))
                    AudioManager.StopVoice();
                AudioManager.PlayVoiceAsync(AutoVoicePath).Forget();
            }

            var shouldReset = (!Assigned(ResetPrinter) && printerMeta.AutoReset) || (Assigned(ResetPrinter) && ResetPrinter.Value);
            var shouldAddBacklog = shouldReset || string.IsNullOrEmpty(printer.Text);
            if (shouldReset)
            {
                printer.Text = string.Empty;
                printer.RevealProgress = 0f;
            }

            if ((Assigned(LineBreaks) && LineBreaks > 0) || (!Assigned(LineBreaks) && printerMeta.AutoLineBreak > 0 && !string.IsNullOrWhiteSpace(printer.Text)))
                await new AppendLineBreak { PrinterId = printer.Id, AuthorId = AuthorId, Count = Assigned(LineBreaks) ? LineBreaks.Value : printerMeta.AutoLineBreak }.ExecuteAsync();

            var textToPrint = Text.Value; // Copy to a temp var to prevent multiple evaluations of dynamic values.

            var continueInputCts = InputManager.GetContinue()?.GetInputStartCancellationToken();
            var skipInputCts = InputManager.GetSkip()?.GetInputStartCancellationToken();
            using (var printCts = CancellationTokenSource.CreateLinkedTokenSource(continueInputCts ?? default, skipInputCts ?? default, cancellationToken))
            {
                await PrinterManager.PrintTextAsync(printer.Id, textToPrint, AuthorId, RevealSpeed, printCts.Token);
                if (cancellationToken.IsCancellationRequested) return;

                printer.RevealProgress = 1f; // Make sure all the text is always revealed.

                await AsyncUtils.WaitEndOfFrame; // Always wait at least one frame to prevent instant skipping.
                if (cancellationToken.IsCancellationRequested) return;

                if ((!Assigned(WaitForInput) && printerMeta.AutoWait) || (Assigned(WaitForInput) && WaitForInput.Value))
                {
                    if (ScriptPlayer.AutoPlayActive) // Add delay per printed chars when in auto mode.
                    {
                        var baseDelay = Configuration.ScaleAutoWait ? PrinterManager.BaseAutoDelay * RevealSpeed : PrinterManager.BaseAutoDelay;
                        var autoPlayDelay = Mathf.Lerp(0, Configuration.MaxAutoWaitDelay, baseDelay) * textToPrint.Count(char.IsLetterOrDigit);
                        var waitUntilTime = Time.time + autoPlayDelay;
                        while (Time.time < waitUntilTime && !printCts.Token.IsCancellationRequested)
                            await AsyncUtils.WaitEndOfFrame;
                        if (cancellationToken.IsCancellationRequested) return;
                    }

                    ScriptPlayer.SetWaitingForInputEnabled(true);
                }

                var backlog = Engine.GetService<IUIManager>().GetUI<UI.IBacklogUI>();
                if (shouldAddBacklog) backlog?.AddMessage(textToPrint, AuthorId, PlayAutoVoice ? AutoVoicePath : null);
                else backlog?.AppendMessage(textToPrint, PlayAutoVoice ? AutoVoicePath : null);
            }
        }
    } 
}
