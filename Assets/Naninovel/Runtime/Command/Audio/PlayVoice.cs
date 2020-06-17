// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Plays a voice clip at the provided path.
    /// </summary>
    [CommandAlias("voice")]
    public class PlayVoice : AudioCommand, Command.IPreloadable
    {
        /// <summary>
        /// Path to the voice clip to play.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter VoicePath;
        /// <summary>
        /// Volume of the playback.
        /// </summary>
        public DecimalParameter Volume = 1f;
        /// <summary>
        /// Audio mixer [group path](https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.FindMatchingGroups) that should be used when playing the audio.
        /// </summary>
        [ParameterAlias("group")]
        public StringParameter GroupPath;

        public async UniTask HoldResourcesAsync ()
        {
            if (!Assigned(VoicePath) || VoicePath.DynamicValue) return;
            await AudioManager.HoldVoiceResourcesAsync(this, VoicePath);
        }

        public void ReleaseResources ()
        {
            if (!Assigned(VoicePath) || VoicePath.DynamicValue) return;
            AudioManager.ReleaseVoiceResources(this, VoicePath);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            await AudioManager.PlayVoiceAsync(VoicePath, Volume, GroupPath);
        }
    } 
}
