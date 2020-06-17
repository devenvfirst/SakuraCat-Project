// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Plays or modifies currently played [SFX (sound effect)](/guide/audio.md#sound-effects) track with the provided name.
    /// </summary>
    /// <remarks>
    /// Sound effect tracks are not looped by default.
    /// When sfx track name (SfxPath) is not specified, will affect all the currently played tracks.
    /// When invoked for a track that is already playing, the playback won't be affected (track won't start playing from the start),
    /// but the specified parameters (volume and whether the track is looped) will be applied.
    /// </remarks>
    /// <example>
    /// ; Plays an SFX with the name `Explosion` once
    /// @sfx Explosion
    /// 
    /// ; Plays an SFX with the name `Rain` in a loop and fades-in over 30 seconds
    /// @sfx Rain loop:true fade:30
    /// 
    /// ; Changes volume of all the played SFX tracks to 75% over 2.5 seconds and disables looping for all of them
    /// @sfx volume:0.75 loop:false time:2.5
    /// </example>
    [CommandAlias("sfx")]
    public class PlaySfx : AudioCommand, Command.IPreloadable
    {
        /// <summary>
        /// Path to the sound effect asset to play.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter SfxPath;
        /// <summary>
        /// Volume of the sound effect.
        /// </summary>
        public DecimalParameter Volume = 1f;
        /// <summary>
        /// Whether to play the sound effect in a loop.
        /// </summary>
        public BooleanParameter Loop = false;
        /// <summary>
        /// Duration of the volume fade-in when starting playback, in seconds (0.0 by default); 
        /// doesn't have effect when modifying a playing track.
        /// </summary>
        [ParameterAlias("fade")]
        public DecimalParameter FadeInDuration = 0f;
        /// <summary>
        /// Audio mixer [group path](https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.FindMatchingGroups) that should be used when playing the audio.
        /// </summary>
        [ParameterAlias("group")]
        public StringParameter GroupPath;
        /// <summary>
        /// Duration (in seconds) of the modification. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        public async UniTask HoldResourcesAsync ()
        {
            if (!Assigned(SfxPath) || SfxPath.DynamicValue) return;
            await AudioManager.HoldAudioResourcesAsync(this, SfxPath);
        }

        public void ReleaseResources ()
        {
            if (!Assigned(SfxPath) || SfxPath.DynamicValue) return;
            AudioManager.ReleaseAudioResources(this, SfxPath);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (Assigned(SfxPath)) await PlayOrModifyTrackAsync(AudioManager, SfxPath, Volume, Loop, Duration, FadeInDuration, GroupPath, cancellationToken);
            else await UniTask.WhenAll(AudioManager.GetPlayedSfxPaths().ToList().Select(path => PlayOrModifyTrackAsync(AudioManager, path, Volume, Loop, Duration, FadeInDuration, null, cancellationToken)));
        }

        private static async UniTask PlayOrModifyTrackAsync (IAudioManager mngr, string path, float volume, bool loop, float time, float fade, string group, CancellationToken cancellationToken)
        {
            if (mngr.SfxPlaying(path)) await mngr.ModifySfxAsync(path, volume, loop, time, cancellationToken);
            else await mngr.PlaySfxAsync(path, volume, fade, loop, group, cancellationToken);
        }
    } 
}
