// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Video;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IBackgroundActor"/> implementation using <see cref="VideoPlayer"/> to represent the actor.
    /// </summary>
    public class VideoBackground : MonoBehaviourActor, IBackgroundActor
    {
        private class VideoData { public VideoPlayer Player; public RenderTexture RenderTexture; }

        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected TransitionalSpriteRenderer SpriteRenderer { get; }

        private static bool sharedResourcesInitialized;
        private static int sharedRefCounter;
        private static RenderTextureDescriptor renderTextureDescriptor;
        private static LiteralMap<VideoData> videoDataMap;
        private static Vector2Int referenceResolution;

        private string appearance;
        private bool visible;
        private LocalizableResourceLoader<VideoClip> videoLoader;

        public VideoBackground (string id, BackgroundMetadata metadata)
            : base(id, metadata)
        {
            if (referenceResolution == default)
                referenceResolution = Engine.GetConfiguration<CameraConfiguration>().ReferenceResolution;

            InitializeSharedResources();
            sharedRefCounter++;

            var providerMngr = Engine.GetService<IResourceProviderManager>();
            var localeMngr = Engine.GetService<ILocalizationManager>();
            videoLoader = new LocalizableResourceLoader<VideoClip>(
                providerMngr.GetProviders(metadata.Loader.ProviderTypes), 
                localeMngr, $"{metadata.Loader.PathPrefix}/{id}");

            SpriteRenderer = GameObject.AddComponent<TransitionalSpriteRenderer>();
            SpriteRenderer.Pivot = metadata.Pivot;
            SpriteRenderer.PixelsPerUnit = metadata.PixelsPerUnit;
            SpriteRenderer.DepthPassEnabled = metadata.EnableDepthPass;
            SpriteRenderer.DepthAlphaCutoff = metadata.DepthAlphaCutoff;
            SpriteRenderer.CustomShader = metadata.CustomShader;

            SetVisibility(false);
        }

        public override async UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance)) return;

            if (transition.HasValue) SpriteRenderer.Transition = transition.Value;

            var videoData = await GetOrLoadVideoDataAsync(appearance);
            if (cancellationToken.IsCancellationRequested) return;
            if (!videoData.Player.isPrepared)
            {
                videoData.Player.Prepare();
                while (!videoData.Player.isPrepared) 
                    await AsyncUtils.WaitEndOfFrame;
                if (cancellationToken.IsCancellationRequested) return;
            }
            videoData.Player.Play();

            await SpriteRenderer.TransitionToAsync(videoData.RenderTexture, duration, easingType, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            foreach (var kv in videoDataMap) // Make sure no other videos are playing.
                if (!kv.Key.EqualsFast(appearance))
                    kv.Value.Player.Stop();
        }

        public override async UniTask ChangeVisibilityAsync (bool isVisible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            this.visible = isVisible;

            await SpriteRenderer.FadeToAsync(isVisible ? 1 : 0, duration, easingType, cancellationToken);
        }

        public override async UniTask HoldResourcesAsync (object holder, string appearance)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            await GetOrLoadVideoDataAsync(appearance);

            // Releasing is done in Dispose().
            videoLoader.GetLoadedOrNull(appearance)?.Hold(this);
        }

        public override void Dispose ()
        {
            base.Dispose();

            videoLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
            sharedRefCounter--;
            DestroySharedResources();
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibilityAsync(visible, 0).Forget();

        protected override Color GetBehaviourTintColor () => Color.white;

        protected override void SetBehaviourTintColor (Color tintColor) { }

        private async UniTask<VideoData> GetOrLoadVideoDataAsync (string videoName)
        {
            if (videoDataMap.ContainsKey(videoName)) return videoDataMap[videoName];

            var renderTexture = new RenderTexture(renderTextureDescriptor);
            var videoPlayer = Engine.CreateObject<VideoPlayer>(videoName);
            videoPlayer.playOnAwake = false;

            #if UNITY_WEBGL && !UNITY_EDITOR
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = PathUtils.Combine(Application.streamingAssetsPath, videoLoader.BuildFullPath(videoName)) + ".mp4";
            await AsyncUtils.WaitEndOfFrame;
            #else
            var videoClip = await videoLoader.LoadAsync(videoName);
            if (!videoClip.IsValid) Debug.LogError($"Failed to load `{videoName}` resource for `{Id}` video background actor. Make sure the video clip is assigned in the actor resources.");
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
            #endif

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            var sceneData = new VideoData { Player = videoPlayer, RenderTexture = renderTexture };
            videoDataMap[videoName] = sceneData;

            return sceneData;
        }

        private static void InitializeSharedResources ()
        {
            if (sharedResourcesInitialized) return;

            renderTextureDescriptor = new RenderTextureDescriptor(referenceResolution.x, referenceResolution.y, RenderTextureFormat.Default);
            videoDataMap = new LiteralMap<VideoData>();
            sharedResourcesInitialized = true;
        }

        private static void DestroySharedResources ()
        {
            if (sharedRefCounter > 0) return;

            foreach (var videoData in videoDataMap.Values)
            {
                videoData.Player.Stop();
                if (Application.isPlaying)
                {
                    Object.Destroy(videoData.Player.gameObject);
                    Object.Destroy(videoData.RenderTexture);
                }
                else
                {
                    Object.DestroyImmediate(videoData.Player.gameObject);
                    Object.DestroyImmediate(videoData.RenderTexture);
                }
            }

            sharedResourcesInitialized = false;
        }
    }
}
