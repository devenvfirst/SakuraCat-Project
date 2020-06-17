// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IBackgroundActor"/> implementation using <see cref="Scene"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// The implementation currently requires scenes to be at `./Assets/Scenes` project folder; resource providers are not supported.
    /// Scenes should be added to the build settings.
    /// </remarks>
    public class SceneBackground : MonoBehaviourActor, IBackgroundActor
    {
        private class SceneData { public Scene Scene; public GameObject RootObject; public RenderTexture RenderTexture; }

        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected TransitionalSpriteRenderer SpriteRenderer { get; }

        private const string pathPrefix = "Assets/Scenes/";
        private static bool sharedResourcesInitialized;
        private static int sharedRefCounter;
        private static RenderTextureDescriptor renderTextureDescriptor;
        private static LiteralMap<SceneData> sceneDataMap;
        private static Vector2Int referenceResolution;

        private string appearance;
        private bool visible;

        public SceneBackground (string id, BackgroundMetadata metadata) 
            : base(id, metadata)
        {
            if (referenceResolution == default)
                referenceResolution = Engine.GetConfiguration<CameraConfiguration>().ReferenceResolution;

            InitializeSharedResources();
            sharedRefCounter++;

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

            var scene = await GetOrLoadSceneDataAsync(appearance);
            if (cancellationToken.IsCancellationRequested) return;

            await SpriteRenderer.TransitionToAsync(scene.RenderTexture, duration, easingType, cancellationToken: cancellationToken);
        }

        public override async UniTask ChangeVisibilityAsync (bool isVisible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            this.visible = isVisible;

            await SpriteRenderer.FadeToAsync(isVisible ? 1 : 0, duration, easingType, cancellationToken);
        }

        public override async UniTask HoldResourcesAsync (object holder, string appearance)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            await GetOrLoadSceneDataAsync(appearance);

            // Releasing is managed by Dispose().
        }

        public override void Dispose ()
        {
            base.Dispose();

            sharedRefCounter--;
            DestroySharedResources();
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibilityAsync(visible, 0).Forget();

        protected override Color GetBehaviourTintColor () => Color.white;

        protected override void SetBehaviourTintColor (Color tintColor) { }

        private async UniTask<SceneData> GetOrLoadSceneDataAsync (string sceneName)
        {
            if (sceneDataMap.ContainsKey(sceneName)) return sceneDataMap[sceneName];

            var scenePath = string.Concat(pathPrefix, sceneName, ".unity");

            // Load scene.
            await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(scenePath);
            Debug.Assert(scene.isLoaded, $"Failed loading scene `{scenePath}`. Make sure the scene is added to the build settings and located at `{pathPrefix}`.");

            // Add scene's root objects to new single root object.
            var rootObject = new GameObject(scenePath);
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            foreach (var obj in scene.GetRootGameObjects())
                obj.transform.SetParent(rootObject.transform, false);

            // Create render texture and assign to first found camera of the scene's objects.
            var renderTexture = new RenderTexture(renderTextureDescriptor);
            var camera = rootObject.GetComponentInChildren<Camera>(false);
            Debug.Assert(camera, $"Camera component is not found in `{scenePath}` scene.");
            camera.targetTexture = renderTexture;

            // Commit shared data.
            var sceneData = new SceneData { Scene = scene, RootObject = rootObject, RenderTexture = renderTexture };
            sceneDataMap[sceneName] = sceneData;

            return sceneData;
        }

        private static void InitializeSharedResources ()
        {
            if (sharedResourcesInitialized) return;

            renderTextureDescriptor = new RenderTextureDescriptor(referenceResolution.x, referenceResolution.y, RenderTextureFormat.Default);
            sceneDataMap = new LiteralMap<SceneData>();
            sharedResourcesInitialized = true;
        }

        private static void DestroySharedResources ()
        {
            if (sharedRefCounter > 0) return;

            foreach (var sceneData in sceneDataMap.Values)
                SceneManager.UnloadSceneAsync(sceneData.Scene);
            sceneDataMap.Clear();

            sharedResourcesInitialized = false;
        }
    }
}
