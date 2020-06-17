// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playing an SFX (sound effect) track with the provided name.
    /// </summary>
    /// <remarks>
    /// When sound effect track name (SfxPath) is not specified, will stop all the currently played tracks.
    /// </remarks>
    /// <example>
    /// ; Stop playing an SFX with the name `Rain`, fading-out for 15 seconds.
    /// @stopSfx Rain fade:15
    /// 
    /// ; Stops all the currently played sound effect tracks
    /// @stopSfx
    /// </example>
    public class StopSfx : AudioCommand
    {
        /// <summary>
        /// Path to the sound effect to stop.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter SfxPath;
        /// <summary>
        /// Duration of the volume fade-out before stopping playback, in seconds (0.35 by default).
        /// </summary>
        [ParameterAlias("fade")]
        public DecimalParameter FadeOutDuration = 0.35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (Assigned(SfxPath)) await AudioManager.StopSfxAsync(SfxPath, FadeOutDuration, cancellationToken);
            else await AudioManager.StopAllSfxAsync(FadeOutDuration, cancellationToken);
        }
    } 
}
