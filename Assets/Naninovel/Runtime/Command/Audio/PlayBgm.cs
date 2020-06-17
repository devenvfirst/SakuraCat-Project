// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Plays or modifies currently played [BGM (background music)](/guide/audio.md#background-music) track with the provided name.
    /// </summary>
    /// <remarks>
    /// Music tracks are looped by default.
    /// When music track name (BgmPath) is not specified, will affect all the currently played tracks.
    /// When invoked for a track that is already playing, the playback won't be affected (track won't start playing from the start),
    /// but the specified parameters (volume and whether the track is looped) will be applied.
    /// </remarks>
    /// <example>
    /// ; Starts playing a music track with the name `Sanctuary` in a loop
    /// @bgm Sanctuary
    /// 
    /// ; Same as above, but fades-in the volume over 10 seconds and plays only once
    /// @bgm Sanctuary fade:10 loop:false
    /// 
    /// ; Changes volume of all the played music tracks to 50% over 2.5 seconds and makes them play in a loop
    /// @bgm volume:0.5 loop:true time:2.5
    /// 
    /// ; Playes `BattleThemeIntro` once and then immediately `BattleThemeMain` in a loop.
    /// @bgm BattleThemeMain intro:BattleThemeIntro
    /// </example>
    [CommandAlias("bgm")]
    public class PlayBgm : AudioCommand, Command.IPreloadable
    {
        /// <summary>
        /// Path to the music track to play.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter BgmPath;
        /// <summary>
        /// Path to the intro music track to play once before the main track (not affected by the loop parameter).
        /// </summary>
        [ParameterAlias("intro")]
        public StringParameter IntroBgmPath;
        /// <summary>
        /// Volume of the music track.
        /// </summary>
        public DecimalParameter Volume = 1f;
        /// <summary>
        /// Whether to play the track from beginning when it finishes.
        /// </summary>
        public BooleanParameter Loop = true;
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
            if (!Assigned(BgmPath) || BgmPath.DynamicValue) return;
            await AudioManager.HoldAudioResourcesAsync(this, BgmPath);

            if (!Assigned(IntroBgmPath) || IntroBgmPath.DynamicValue) return;
            await AudioManager.HoldAudioResourcesAsync(this, IntroBgmPath);
        }

        public void ReleaseResources ()
        {
            if (!Assigned(BgmPath) || BgmPath.DynamicValue) return;
            AudioManager.ReleaseAudioResources(this, BgmPath);

            if (!Assigned(IntroBgmPath) || IntroBgmPath.DynamicValue) return;
            AudioManager.ReleaseAudioResources(this, IntroBgmPath);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (Assigned(BgmPath)) await PlayOrModifyTrackAsync(AudioManager, BgmPath, Volume, Loop, Duration, FadeInDuration, IntroBgmPath, GroupPath, cancellationToken);
            else await UniTask.WhenAll(AudioManager.GetPlayedBgmPaths().ToList().Select(path => PlayOrModifyTrackAsync(AudioManager, path, Volume, Loop, Duration, FadeInDuration, IntroBgmPath, null, cancellationToken)));
        }

        private static async UniTask PlayOrModifyTrackAsync (IAudioManager mngr, string path, float volume, bool loop, float time, float fade, string introPath, string group, CancellationToken cancellationToken)
        {
            if (mngr.BgmPlaying(path)) await mngr.ModifyBgmAsync(path, volume, loop, time, cancellationToken);
            else await mngr.PlayBgmAsync(path, volume, fade, loop, introPath, group, cancellationToken);
        }
    } 
}
