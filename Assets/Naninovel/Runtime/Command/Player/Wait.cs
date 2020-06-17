// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Holds script execution until the specified wait condition.
    /// </summary>
    /// <example>
    /// ; "ThunderSound" SFX will play 0.5 seconds after the shake background effect finishes.
    /// @fx ShakeBackground
    /// @wait 0.5
    /// @sfx ThunderSound
    /// 
    /// ; Print first two words, then wait for user input before printing the remaining phrase.
    /// Lorem ipsum[wait i] dolor sit amet.
    /// ; You can also use the following shortcut (@i command) for this wait mode.
    /// Lorem ipsum[i] dolor sit amet.
    /// 
    /// ; Start an SFX, print a message and wait for a skippable 5 seconds delay, then stop the SFX.
    /// @sfx Noise loop:true
    /// Jeez, what a disgusting noise. Shut it down![wait i5][skipInput]
    /// @stopSfx Noise
    /// </example>
    public class Wait : Command, Command.IForceWait
    {
        /// <summary>
        /// Literal used to indicate "wait-for-input" mode.
        /// </summary>
        public const string InputLiteral = "i";

        /// <summary>
        /// Wait conditions:<br/>
        ///  - `i` user press continue or skip input key;<br/>
        ///  - `0.0` timer (seconds);<br/>
        ///  - `i0.0` timer, that is skippable by continue or skip input keys.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter WaitMode;

        private static IInputManager inputManager => Engine.GetService<IInputManager>();
        private static IScriptPlayer scriptPlayer => Engine.GetService<IScriptPlayer>();

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            // Don't just return here if skip is enabled; state snapshot is marked as allowed for player rollback when setting waiting for input.

            if (!Assigned(WaitMode))
            {
                LogWarningWithPosition($"`{nameof(WaitMode)}` parameter is not specified, the wait command will do nothing.");
                return;
            }

            // Waiting for player input.
            if (WaitMode.Value.EqualsFastIgnoreCase(InputLiteral))
            {
                scriptPlayer.SetWaitingForInputEnabled(true);
                return;
            }

            // Waiting for timer or input.
            if (WaitMode.Value.StartsWithFast(InputLiteral) && ParseUtils.TryInvariantFloat(WaitMode.Value.GetAfterFirst(InputLiteral), out var skippableWaitTime))
            {
                scriptPlayer.SetWaitingForInputEnabled(true);
                if (scriptPlayer.SkipActive) return;

                var startTime = Time.time;
                while (Application.isPlaying)
                {
                    await AsyncUtils.WaitEndOfFrame;
                    if (cancellationToken.IsCancellationRequested) return;
                    var waitedEnough = (Time.time - startTime) >= skippableWaitTime;
                    var inputActivated = (inputManager.GetContinue()?.StartedDuringFrame ?? false) || (inputManager.GetSkip()?.StartedDuringFrame ?? false);
                    if (waitedEnough || inputActivated) break;
                }
                scriptPlayer.SetWaitingForInputEnabled(false);
                return;
            }

            // Waiting for timer.
            if (ParseUtils.TryInvariantFloat(WaitMode, out var waitTime))
            {
                if (scriptPlayer.SkipActive) return;

                var startTime = Time.time;
                while (Application.isPlaying)
                {
                    await AsyncUtils.WaitEndOfFrame;
                    var waitedEnough = (Time.time - startTime) >= waitTime;
                    if (cancellationToken.IsCancellationRequested || waitedEnough) break;
                }
                return;
            }

            LogWarningWithPosition($"Failed to resolve value of the `{nameof(WaitMode)}` parameter for the wait command. Check the API reference for list of supported values.");
        }
    } 
}
