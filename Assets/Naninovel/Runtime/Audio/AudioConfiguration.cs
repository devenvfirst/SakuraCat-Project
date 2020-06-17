// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;
using UnityEngine.Audio;

namespace Naninovel
{
    [System.Serializable]
    public class AudioConfiguration : Configuration
    {
        public const string DefaultAudioPathPrefix = "Audio";
        public const string DefaultVoicePathPrefix = "Voice";
        public const string DefaultMixerResourcesPath = "Naninovel/DefaultMixer";
        public const string AutoVoiceClipNameTemplate = "{0}/{1}.{2}";

        [Tooltip("Configuration of the resource loader used with audio (BGM and SFX) resources.")]
        public ResourceLoaderConfiguration AudioLoader = new ResourceLoaderConfiguration { PathPrefix = DefaultAudioPathPrefix };
        [Tooltip("Configuration of the resource loader used with voice resources.")]
        public ResourceLoaderConfiguration VoiceLoader = new ResourceLoaderConfiguration { PathPrefix = DefaultVoicePathPrefix };
        [Range(0f, 1f), Tooltip("Master volume to set when the game is first started.")]
        public float DefaultMasterVolume = 1f;
        [Range(0f, 1f), Tooltip("BGM volume to set when the game is first started.")]
        public float DefaultBgmVolume = 1f;
        [Range(0f, 1f), Tooltip("SFX volume to set when the game is first started.")]
        public float DefaultSfxVolume = 1f;
        [Range(0f, 1f), Tooltip("Voice volume to set when the game is first started.")]
        public float DefaultVoiceVolume = 1f;
        [Tooltip("When enabled, each `" + nameof(Commands.PrintText) + "` command will attempt to play voice clip at `VoiceResourcesPrefix/ScriptName/LineIndex.ActionIndex`.")]
        public bool EnableAutoVoicing = false;
        [Tooltip("Dictates how to handle concurrent voices playback:" +
            "\n • Allow Overlap — Concurrent voices will be played without limitation." +
            "\n • Prevent Overlap — Prevent concurrent voices playback by stopping any played voice clip before playing a new one." +
            "\n • Prevent Character Overlap — Prevent concurrent voices playback per character; voices of different characters (auto voicing) and any number of [@voice] command are allowed to be played concurrently.")]
        public VoiceOverlapPolicy VoiceOverlapPolicy = VoiceOverlapPolicy.PreventOverlap;

        [Header("Audio Mixer")]
        [Tooltip("Audio mixer to control audio groups. When not provided, will use a default one.")]
        public AudioMixer CustomAudioMixer = default;
        [Tooltip("Name of the mixer's handle (exposed parameter) to control master volume.")]
        public string MasterVolumeHandleName = "Master Volume";
        [Tooltip("Path of the mixer's group to control master volume.")]
        public string BgmGroupPath = "Master/BGM";
        [Tooltip("Name of the mixer's handle (exposed parameter) to control background music volume.")]
        public string BgmVolumeHandleName = "BGM Volume";
        [Tooltip("Path of the mixer's group to control background music volume.")]
        public string SfxGroupPath = "Master/SFX";
        [Tooltip("Name of the mixer's handle (exposed parameter) to control sound effects volume.")]
        public string SfxVolumeHandleName = "SFX Volume";
        [Tooltip("Path of the mixer's group to control sound effects volume.")]
        public string VoiceGroupPath = "Master/Voice";
        [Tooltip("Name of the mixer's handle (exposed parameter) to control voice volume.")]
        public string VoiceVolumeHandleName = "Voice Volume";

        public static string GetAutoVoiceClipPath (PlaybackSpot playbackSpot)
        {
            return string.Format(AutoVoiceClipNameTemplate, playbackSpot.ScriptName, playbackSpot.LineNumber, playbackSpot.InlineIndex);
        }
    }
}
