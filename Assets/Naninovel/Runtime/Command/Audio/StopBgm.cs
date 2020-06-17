// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playing a BGM (background music) track with the provided name.
    /// </summary>
    /// <remarks>
    /// When music track name (BgmPath) is not specified, will stop all the currently played tracks.
    /// </remarks>
    /// <example>
    /// ; Fades-out the `Promenade` music track over 10 seconds and stops the playback
    /// @stopBgm Promenade fade:10
    /// 
    /// ; Stops all the currently played music tracks
    /// @stopBgm 
    /// </example>
    public class StopBgm : AudioCommand
    {
        /// <summary>
        /// Path to the music track to stop.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter BgmPath;
        /// <summary>
        /// Duration of the volume fade-out before stopping playback, in seconds (0.35 by default).
        /// </summary>
        [ParameterAlias("fade")]
        public DecimalParameter FadeOutDuration = 0.35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (Assigned(BgmPath)) await AudioManager.StopBgmAsync(BgmPath, FadeOutDuration, cancellationToken);
            else await AudioManager.StopAllBgmAsync(FadeOutDuration, cancellationToken);
        }
    } 
}
