// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Audio;

namespace Naninovel
{
    /// <inheritdoc cref="IAudioManager"/>
    [InitializeAtRuntime]
    public class AudioManager : IStatefulService<SettingsStateMap>, IStatefulService<GameStateMap>, IAudioManager
    {
        [System.Serializable]
        public class Settings
        {
            public float MasterVolume;
            public float BgmVolume;
            public float SfxVolume;
            public float VoiceVolume;
        }

        [System.Serializable]
        public class GameState { public List<ClipState> BgmClips; public List<ClipState> SfxClips; }

        [System.Serializable]
        public struct ClipState { public string Path; public float Volume; public bool IsLooped; }

        public AudioConfiguration Configuration { get; }
        public AudioMixer AudioMixer { get; private set; }
        public float MasterVolume { get => GetMixerVolume(Configuration.MasterVolumeHandleName); set => SetMixerVolume(Configuration.MasterVolumeHandleName, value); }
        public float BgmVolume { get => GetMixerVolume(Configuration.BgmVolumeHandleName); set { if (BgmGroupAvailable) SetMixerVolume(Configuration.BgmVolumeHandleName, value); } }
        public float SfxVolume { get => GetMixerVolume(Configuration.SfxVolumeHandleName); set { if (SfxGroupAvailable) SetMixerVolume(Configuration.SfxVolumeHandleName, value); } }
        public float VoiceVolume { get => GetMixerVolume(Configuration.VoiceVolumeHandleName); set { if (VoiceGroupAvailable) SetMixerVolume(Configuration.VoiceVolumeHandleName, value); } }

        protected bool BgmGroupAvailable => bgmGroup;
        protected bool SfxGroupAvailable => sfxGroup;
        protected bool VoiceGroupAvailable => voiceGroup;

        private readonly IResourceProviderManager providersManager;
        private readonly ILocalizationManager localizationManager;
        private readonly Dictionary<string, ClipState> bgmMap, sfxMap;
        private readonly AudioMixerGroup bgmGroup, sfxGroup, voiceGroup;
        private LocalizableResourceLoader<AudioClip> audioLoader, voiceLoader;
        private AudioController audioController;
        private ClipState? voiceClip;

        public AudioManager (AudioConfiguration config, IResourceProviderManager providersManager, ILocalizationManager localizationManager)
        {
            Configuration = config;
            this.providersManager = providersManager;
            this.localizationManager = localizationManager;

            AudioMixer = ObjectUtils.IsValid(config.CustomAudioMixer) ? config.CustomAudioMixer : Resources.Load<AudioMixer>(AudioConfiguration.DefaultMixerResourcesPath);

            if (ObjectUtils.IsValid(AudioMixer))
            {
                bgmGroup = AudioMixer.FindMatchingGroups(config.BgmGroupPath)?.FirstOrDefault();
                sfxGroup = AudioMixer.FindMatchingGroups(config.SfxGroupPath)?.FirstOrDefault();
                voiceGroup = AudioMixer.FindMatchingGroups(config.VoiceGroupPath)?.FirstOrDefault();
            }

            bgmMap = new Dictionary<string, ClipState>();
            sfxMap = new Dictionary<string, ClipState>();
        }

        public UniTask InitializeServiceAsync ()
        {
            audioLoader = Configuration.AudioLoader.CreateLocalizableFor<AudioClip>(providersManager, localizationManager);
            voiceLoader = Configuration.VoiceLoader.CreateLocalizableFor<AudioClip>(providersManager, localizationManager);
            audioController = Engine.CreateObject<AudioController>();

            return UniTask.CompletedTask;
        }

        public void ResetService ()
        {
            audioController.StopAllClips();
            bgmMap.Clear();
            sfxMap.Clear();
            voiceClip = null;

            audioLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
            voiceLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        public void DestroyService ()
        {
            if (audioController)
            {
                audioController.StopAllClips();
                Object.Destroy(audioController.gameObject);
            }

            audioLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
            voiceLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        public void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                MasterVolume = MasterVolume,
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                VoiceVolume = VoiceVolume
            };
            stateMap.SetState(settings);
        }

        public UniTask LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>();

            if (settings is null) // Apply default settings.
            {
                MasterVolume = Configuration.DefaultMasterVolume;
                BgmVolume = Configuration.DefaultBgmVolume;
                SfxVolume = Configuration.DefaultSfxVolume;
                VoiceVolume = Configuration.DefaultVoiceVolume;
                return UniTask.CompletedTask;
            }

            MasterVolume = settings.MasterVolume;
            BgmVolume = settings.BgmVolume;
            SfxVolume = settings.SfxVolume;
            VoiceVolume = settings.VoiceVolume;
            return UniTask.CompletedTask;
        }

        public void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState() { // Save only looped audio to prevent playing multiple clips at once when the game is (auto) saved in skip mode.
                BgmClips = bgmMap.Values.Where(s => BgmPlaying(s.Path) && s.IsLooped).ToList(),
                SfxClips = sfxMap.Values.Where(s => SfxPlaying(s.Path) && s.IsLooped).ToList()
            };
            stateMap.SetState(state);
        }

        public async UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>() ?? new GameState();
            var tasks = new List<UniTask>();

            if (state.BgmClips != null && state.BgmClips.Count > 0)
            {
                foreach (var bgmPath in bgmMap.Keys.ToList())
                    if (!state.BgmClips.Exists(c => c.Path.EqualsFast(bgmPath)))
                        tasks.Add(StopBgmAsync(bgmPath));
                foreach (var clipState in state.BgmClips)
                    if (BgmPlaying(clipState.Path))
                        tasks.Add(ModifyBgmAsync(clipState.Path, clipState.Volume, clipState.IsLooped, 0));
                    else tasks.Add(PlayBgmAsync(clipState.Path, clipState.Volume, 0, clipState.IsLooped));
            }
            else tasks.Add(StopAllBgmAsync());

            if (state.SfxClips != null && state.SfxClips.Count > 0)
            {
                foreach (var sfxPath in sfxMap.Keys.ToList())
                    if (!state.SfxClips.Exists(c => c.Path.EqualsFast(sfxPath)))
                        tasks.Add(StopSfxAsync(sfxPath));
                foreach (var clipState in state.SfxClips)
                    if (SfxPlaying(clipState.Path))
                        tasks.Add(ModifySfxAsync(clipState.Path, clipState.Volume, clipState.IsLooped, 0));
                    else tasks.Add(PlaySfxAsync(clipState.Path, clipState.Volume, 0, clipState.IsLooped));
            }
            else tasks.Add(StopAllSfxAsync());

            await UniTask.WhenAll(tasks);
        }

        public async UniTask HoldAudioResourcesAsync (object holder, string path)
        {
            var resource = await audioLoader.LoadAsync(path);
            if (resource.IsValid)
                resource.Hold(holder);
        }

        public void ReleaseAudioResources (object holder, string path)
        {
            if (!audioLoader.IsLoaded(path)) return;

            var resource = audioLoader.GetLoadedOrNull(path);
            resource.Release(holder, false);
            if (resource.HoldersCount == 0)
            {
                audioController.StopClip(resource);
                resource.Provider.UnloadResource(resource.Path);
            }
        }

        public async UniTask HoldVoiceResourcesAsync (object holder, string path)
        {
            var resource = await voiceLoader.LoadAsync(path);
            if (resource.IsValid)
                resource.Hold(holder);
        }

        public void ReleaseVoiceResources (object holder, string path)
        {
            if (!voiceLoader.IsLoaded(path)) return;

            var resource = voiceLoader.GetLoadedOrNull(path);
            resource.Release(holder, false);
            if (resource.HoldersCount == 0)
            {
                audioController.StopClip(resource);
                resource.Provider.UnloadResource(resource.Path);
            }
        }

        public bool BgmPlaying (string path)
        {
            if (!bgmMap.ContainsKey(path)) return false;
            return IsAudioPlaying(path);
        }

        public bool SfxPlaying (string path)
        {
            if (!sfxMap.ContainsKey(path)) return false;
            return IsAudioPlaying(path);
        }

        public bool VoicePlaying (string path)
        {
            if (!voiceClip.HasValue || voiceClip.Value.Path != path) return false;
            if (!voiceLoader.IsLoaded(path)) return false;
            var clipResource = voiceLoader.GetLoadedOrNull(path);
            if (!clipResource.IsValid) return false;
            return audioController.GetTrack(clipResource)?.Playing ?? false;
        }

        public IEnumerable<string> GetPlayedBgmPaths () => bgmMap.Keys;

        public IEnumerable<string> GetPlayedSfxPaths () => sfxMap.Keys;

        public string GetPlayedVoicePath () => VoicePlaying(voiceClip?.Path) ? voiceClip.Value.Path : null;

        public async UniTask<bool> AudioExistsAsync (string path) => await audioLoader.ExistsAsync(path);

        public async UniTask<bool> VoiceExistsAsync (string path) => await voiceLoader.ExistsAsync(path);

        public async UniTask ModifyBgmAsync (string path, float volume, bool loop, float time, CancellationToken cancellationToken = default)
        {
            if (!bgmMap.ContainsKey(path)) return;

            var state = bgmMap[path];
            state.Volume = volume;
            state.IsLooped = loop;
            bgmMap[path] = state;
            await ModifyAudioAsync(path, volume, loop, time, cancellationToken);
        }

        public async UniTask ModifySfxAsync (string path, float volume, bool loop, float time, CancellationToken cancellationToken = default)
        {
            if (!sfxMap.ContainsKey(path)) return;

            var state = sfxMap[path];
            state.Volume = volume;
            state.IsLooped = loop;
            sfxMap[path] = state;
            await ModifyAudioAsync(path, volume, loop, time, cancellationToken);
        }

        public void PlaySfxFast (string path, float volume = 1f, string group = default, bool restartIfPlaying = true)
        {
            if (!audioLoader.IsLoaded(path))
            {
                Debug.LogError($"Failed to fast-play `{path}` SFX: the resource is not loaded.");
                return;
            }
            var clip = audioLoader.GetLoadedOrNull(path);
            if (!restartIfPlaying && audioController.ClipPlaying(clip)) return;
            audioController.PlayClip(clip, null, volume, false, FindAudioGroupOrDefault(group, sfxGroup));
        }

        public async UniTask PlayBgmAsync (string path, float volume = 1f, float fadeTime = 0f, bool loop = true, string introPath = null, string group = default, CancellationToken cancellationToken = default)
        {
            var clipResource = await audioLoader.LoadAsync(path);
            if (cancellationToken.IsCancellationRequested) return;
            if (!clipResource.IsValid)
            {
                Debug.LogWarning($"Failed to play BGM `{path}`: resource not found.");
                return;
            }
            clipResource.Hold(this);

            bgmMap[path] = new ClipState { Path = path, Volume = volume, IsLooped = loop };

            var introClip = default(AudioClip);
            if (!string.IsNullOrEmpty(introPath))
            {
                var introClipResource = await audioLoader.LoadAsync(introPath);
                if (!introClipResource.IsValid)
                    Debug.LogWarning($"Failed to load intro BGM `{path}`: resource not found.");
                else
                {
                    introClipResource.Hold(this);
                    introClip = introClipResource.Object;
                }
            }

            if (fadeTime <= 0) audioController.PlayClip(clipResource, null, volume, loop, FindAudioGroupOrDefault(group, bgmGroup), introClip);
            else await audioController.PlayClipAsync(clipResource, fadeTime, null, volume, loop, FindAudioGroupOrDefault(group, bgmGroup), introClip, cancellationToken);
        }

        public async UniTask StopBgmAsync (string path, float fadeTime = 0f, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (bgmMap.ContainsKey(path))
                bgmMap.Remove(path);

            if (!audioLoader.IsLoaded(path)) return;
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (fadeTime <= 0) audioController.StopClip(clipResource);
            else await audioController.StopClipAsync(clipResource, fadeTime, cancellationToken);

            if (!BgmPlaying(path))
                clipResource?.Release(this);
        }

        public async UniTask StopAllBgmAsync (float fadeTime = 0f, CancellationToken cancellationToken = default)
        {
            await UniTask.WhenAll(bgmMap.Keys.ToList().Select(p => StopBgmAsync(p, fadeTime, cancellationToken)));
        }

        public async UniTask PlaySfxAsync (string path, float volume = 1f, float fadeTime = 0f, bool loop = false, string group = default, CancellationToken cancellationToken = default)
        {
            var clipResource = await audioLoader.LoadAsync(path);
            if (cancellationToken.IsCancellationRequested) return;
            if (!clipResource.IsValid)
            {
                Debug.LogWarning($"Failed to play SFX `{path}`: resource not found.");
                return;
            }

            sfxMap[path] = new ClipState { Path = path, Volume = volume, IsLooped = loop };

            clipResource.Hold(this);

            if (fadeTime <= 0) audioController.PlayClip(clipResource, null, volume, loop, FindAudioGroupOrDefault(group, sfxGroup));
            else await audioController.PlayClipAsync(clipResource, fadeTime, null, volume, loop, FindAudioGroupOrDefault(group, sfxGroup), cancellationToken: cancellationToken);
        }

        public async UniTask StopSfxAsync (string path, float fadeTime = 0f, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (sfxMap.ContainsKey(path))
                sfxMap.Remove(path);

            if (!audioLoader.IsLoaded(path)) return;
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (fadeTime <= 0) audioController.StopClip(clipResource);
            else await audioController.StopClipAsync(clipResource, fadeTime, cancellationToken);

            if (!SfxPlaying(path))
                clipResource?.Release(this);
        }

        public async UniTask StopAllSfxAsync (float fadeTime = 0f, CancellationToken cancellationToken = default)
        {
            await UniTask.WhenAll(sfxMap.Keys.ToList().Select(p => StopSfxAsync(p, fadeTime, cancellationToken)));
        }

        public async UniTask PlayVoiceAsync (string path, float volume = 1f, string group = default, CancellationToken cancellationToken = default)
        {
            var clipResource = await voiceLoader.LoadAsync(path);
            if (!clipResource.IsValid || cancellationToken.IsCancellationRequested) return;

            if (Configuration.VoiceOverlapPolicy == VoiceOverlapPolicy.PreventOverlap)
                StopVoice();

            voiceClip = new ClipState { Path = path, IsLooped = false, Volume = volume };

            audioController.PlayClip(clipResource, volume: volume, mixerGroup: FindAudioGroupOrDefault(group, voiceGroup));
            clipResource.Hold(this);
        }

        public async UniTask PlayVoiceSequenceAsync (List<string> pathList, float volume = 1f, string group = default, CancellationToken cancellationToken = default)
        {
            foreach (var path in pathList)
            {
                await PlayVoiceAsync(path, volume, group);
                if (cancellationToken.IsCancellationRequested) return;
                await UniTask.WaitWhile(() => VoicePlaying(path));
                if (cancellationToken.IsCancellationRequested) return;
            }
        }

        public void StopVoice ()
        {
            if (!voiceClip.HasValue) return;

            var clipResource = voiceLoader.GetLoadedOrNull(voiceClip.Value.Path);
            voiceClip = null;
            audioController.StopClip(clipResource);
            clipResource?.Release(this);
        }

        public IAudioTrack GetAudioTrack (string path)
        {
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (clipResource is null || !clipResource.IsValid) return null;
            return audioController.GetTrack(clipResource.Object);
        }

        public IAudioTrack GetVoiceTrack (string path)
        {
            var clipResource = voiceLoader.GetLoadedOrNull(path);
            if (clipResource is null || !clipResource.IsValid) return null;
            return audioController.GetTrack(clipResource.Object);
        }

        private bool IsAudioPlaying (string path)
        {
            if (!audioLoader.IsLoaded(path)) return false;
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (!clipResource.IsValid) return false;
            return audioController.GetTrack(clipResource)?.Playing ?? false;
        }

        private async UniTask ModifyAudioAsync (string path, float volume, bool loop, float time, CancellationToken cancellationToken = default)
        {
            if (!audioLoader.IsLoaded(path)) return;
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (!clipResource.IsValid) return;
            var track = audioController.GetTrack(clipResource);
            if (track is null) return;
            track.Loop = loop;
            if (time <= 0) track.Volume = volume;
            else await track.FadeAsync(volume, time, cancellationToken);
        }

        private float GetMixerVolume (string handleName)
        {
            float value;

            if (ObjectUtils.IsValid(AudioMixer))
            {
                AudioMixer.GetFloat(handleName, out value);
                value = MathUtils.DecibelToLinear(value);
            }
            else value = audioController.Volume;

            return value;
        }

        private void SetMixerVolume (string handleName, float value)
        {
            if (ObjectUtils.IsValid(AudioMixer))
                AudioMixer.SetFloat(handleName, MathUtils.LinearToDecibel(value));
            else audioController.Volume = value;
        }

        private AudioMixerGroup FindAudioGroupOrDefault (string path, AudioMixerGroup defaultGroup)
        {
            if (string.IsNullOrEmpty(path)) 
                return defaultGroup;
            var group = AudioMixer.FindMatchingGroups(path)?.FirstOrDefault();
            return ObjectUtils.IsValid(group) ? group : defaultGroup;
        }
    }
}
