// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <inheritdoc cref="IMoviePlayer"/>
    [InitializeAtRuntime]
    public class MoviePlayer : IMoviePlayer
    {
        public event Action OnMoviePlay;
        public event Action OnMovieStop;
        public event Action<Texture> OnMovieTextureReady;

        public MoviesConfiguration Configuration { get; }
        public bool Playing => playCTS != null && !playCTS.IsCancellationRequested;
        public Texture2D FadeTexture { get; private set; }

        protected VideoPlayer Player { get; private set; }

        private const string defaultFadeTextureResourcesPath = "Naninovel/Textures/BlackTexture";

        private IInputManager inputManager;
        private IResourceProviderManager providerManager;
        private ILocalizationManager localeManager;
        private LocalizableResourceLoader<VideoClip> videoLoader;
        private CancellationTokenSource playCTS;
        private string playedMovieName;
        private IInputSampler cancelInput;

        public MoviePlayer (MoviesConfiguration config, IResourceProviderManager providerManager, ILocalizationManager localeManager, IInputManager inputManager)
        {
            this.Configuration = config;
            this.providerManager = providerManager;
            this.localeManager = localeManager;
            this.inputManager = inputManager;

            FadeTexture = ObjectUtils.IsValid(config.CustomFadeTexture) ? config.CustomFadeTexture : Resources.Load<Texture2D>(defaultFadeTextureResourcesPath);
        }

        public UniTask InitializeServiceAsync ()
        {
            videoLoader = Configuration.Loader.CreateLocalizableFor<VideoClip>(providerManager, localeManager);
            cancelInput = inputManager.GetCancel();

            Player = Engine.CreateObject<VideoPlayer>(nameof(MoviePlayer));
            Player.playOnAwake = false;
            Player.skipOnDrop = Configuration.SkipFrames;
            #if UNITY_WEBGL && !UNITY_EDITOR
            Player.source = VideoSource.Url;
            #else
            Player.source = VideoSource.VideoClip;
            #endif
            Player.renderMode = VideoRenderMode.APIOnly;
            Player.isLooping = false;
            Player.audioOutputMode = VideoAudioOutputMode.Direct;
            Player.loopPointReached += HandleLoopPointReached;

            if (cancelInput != null)
                cancelInput.OnStart += Stop;

            return UniTask.CompletedTask;
        }

        public void ResetService ()
        {
            if (Playing) Stop();
            videoLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        public void DestroyService ()
        {
            if (Playing) Stop();
            if (Player != null) Player.loopPointReached -= HandleLoopPointReached;
            if (cancelInput != null) cancelInput.OnStart -= Stop;
            videoLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        public async UniTask PlayAsync (string movieName, CancellationToken cancellationToken = default)
        {
            if (Playing) Stop();

            playedMovieName = movieName;
            playCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            OnMoviePlay?.Invoke();
            await UniTask.Delay(TimeSpan.FromSeconds(Configuration.FadeDuration));
            if (cancellationToken.IsCancellationRequested) return;

            #if UNITY_WEBGL && !UNITY_EDITOR
            Player.url = PathUtils.Combine(Application.streamingAssetsPath, videoLoader.BuildFullPath(movieName)) + ".mp4";
            #else
            var videoClipResource = await videoLoader.LoadAsync(movieName);
            if (cancellationToken.IsCancellationRequested) return;
            if (!videoClipResource.IsValid) { Debug.LogError($"Failed to load `{movieName}` movie."); Stop(); return; }
            Player.clip = videoClipResource;
            videoClipResource.Hold(this);
            #endif

            Player.Prepare();
            while (!Player.isPrepared) await AsyncUtils.WaitEndOfFrame;
            if (cancellationToken.IsCancellationRequested) return;
            OnMovieTextureReady?.Invoke(Player.texture);

            Player.Play();
            while (Playing) await AsyncUtils.WaitEndOfFrame;
        }

        public void Stop ()
        {
            if (Player) Player.Stop();
            playCTS?.Cancel();
            playCTS?.Dispose();
            playCTS = null;

            videoLoader?.GetLoadedOrNull(playedMovieName)?.Release(this);
            playedMovieName = null;

            OnMovieStop?.Invoke();
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        public UniTask HoldResourcesAsync (object holder, string movieName) => UniTask.CompletedTask;
        #else
        public async UniTask HoldResourcesAsync (object holder, string movieName)
        {
            var resource = await videoLoader.LoadAsync(movieName);
            if (resource.IsValid)
                resource.Hold(holder);
        }
        #endif

        public void ReleaseResources (object holder, string movieName)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            return;
            #else
            if (!videoLoader.IsLoaded(movieName)) return;
            var resource = videoLoader.GetLoadedOrNull(movieName);
            resource.Release(holder);
            #endif
        }

        private void HandleLoopPointReached (VideoPlayer source) => Stop();
    }
}
