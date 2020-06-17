// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="MonoBehaviourActor"/> using <see cref="TransitionalSpriteRenderer"/> to represent appearance of the actor.
    /// </summary>
    public abstract class SpriteActor : MonoBehaviourActor
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected LocalizableResourceLoader<Texture2D> AppearanceLoader { get; }
        protected TransitionalSpriteRenderer SpriteRenderer { get; }

        private string appearance;
        private bool visible;
        private Resource<Texture2D> defaultAppearance;

        public SpriteActor (string id, OrthoActorMetadata metadata)
            : base(id, metadata)
        {
            AppearanceLoader = ConstructAppearanceLoader(metadata);

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
            var previousAppearance = this.appearance;
            this.appearance = appearance;

            if (transition.HasValue) SpriteRenderer.Transition = transition.Value;

            var textureResource = string.IsNullOrWhiteSpace(appearance) ? await LoadDefaultAppearanceAsync() : await LoadAppearanceAsync(appearance);
            textureResource?.Hold(this);
            await SpriteRenderer.TransitionToAsync(textureResource, duration, easingType, cancellationToken: cancellationToken);

            // When using `wait:false` this async method won't be waited, which potentially could lead to a situation, where
            // a consequent same method will re-set the currently disposed resource.
            // Here we check that the disposed (previousAppearance) resource is not actually being used right now, before disposing it.
            if (previousAppearance != this.appearance)
                AppearanceLoader?.GetLoadedOrNull(previousAppearance)?.Release(this);
        }

        public override async UniTask ChangeVisibilityAsync (bool isVisible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            // When appearance is not set (and default one is not preloaded for some reason, eg when using dynamic parameters) 
            // and revealing the actor -- attempt to load default appearance texture.
            if (!Visible && isVisible && string.IsNullOrWhiteSpace(Appearance) && (defaultAppearance is null || !defaultAppearance.IsValid))
                await ChangeAppearanceAsync(null, 0, cancellationToken: cancellationToken);

            this.visible = isVisible;

            await SpriteRenderer.FadeToAsync(isVisible ? TintColor.a : 0, duration, easingType, cancellationToken);
        }

        public override async UniTask HoldResourcesAsync (object holder, string appearance)
        {
            if (string.IsNullOrEmpty(appearance))
            {
                await LoadDefaultAppearanceAsync();
                defaultAppearance?.Hold(holder);
                return;
            }

            var resource = await AppearanceLoader.LoadAsync(appearance);
            if (resource.IsValid)
                resource.Hold(holder);
        }

        public override void ReleaseResources (object holder, string appearance)
        {
            if (string.IsNullOrEmpty(appearance)) return;

            AppearanceLoader.GetLoadedOrNull(appearance)?.Release(holder);
        }

        public override void Dispose ()
        {
            base.Dispose();

            AppearanceLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        protected virtual LocalizableResourceLoader<Texture2D> ConstructAppearanceLoader (OrthoActorMetadata metadata)
        {
            var providerMngr = Engine.GetService<IResourceProviderManager>();
            var localeMngr = Engine.GetService<ILocalizationManager>();
            var appearanceLoader = new LocalizableResourceLoader<Texture2D>(
                providerMngr.GetProviders(metadata.Loader.ProviderTypes),
                localeMngr, $"{metadata.Loader.PathPrefix}/{Id}");

            return appearanceLoader;
        }

        protected virtual void SetAppearance (string appearance) => ChangeAppearanceAsync(appearance, 0).Forget();

        protected virtual void SetVisibility (bool visible) => ChangeVisibilityAsync(visible, 0).Forget();

        protected override Color GetBehaviourTintColor () => SpriteRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = SpriteRenderer.TintColor.a;
            SpriteRenderer.TintColor = tintColor;
        }

        protected virtual async UniTask<Resource<Texture2D>> LoadAppearanceAsync (string appearance)
        {
            var texture = await AppearanceLoader.LoadAsync(appearance);

            if (!texture.IsValid)
            {
                Debug.LogWarning($"Failed to load '{appearance}' appearance texture for `{Id}` sprite actor: the resource is not found.");
                return null;
            }

            ApplyTextureSettings(texture);
            return texture;
        }

        protected virtual async UniTask<Resource<Texture2D>> LoadDefaultAppearanceAsync ()
        {
            if (defaultAppearance != null && defaultAppearance.IsValid) return defaultAppearance;

            var defaultTexturePath = await LocateDefaultAppearanceAsync();
            defaultAppearance = defaultTexturePath is null ? new Resource<Texture2D>(null, Texture2D.whiteTexture, null) : await AppearanceLoader.LoadAsync(defaultTexturePath);

            ApplyTextureSettings(defaultAppearance);

            if (!SpriteRenderer.MainTexture)
                SpriteRenderer.MainTexture = defaultAppearance;

            return defaultAppearance;
        }

        protected virtual async UniTask<string> LocateDefaultAppearanceAsync ()
        {
            var texturePaths = (await AppearanceLoader.LocateAsync(string.Empty))?.ToList();
            if (texturePaths != null && texturePaths.Count > 0)
            {
                // Remove path prefix (caller is expecting a local path).
                for (int i = 0; i < texturePaths.Count; i++)
                    if (texturePaths[i].Contains($"{AppearanceLoader.PathPrefix}/"))
                        texturePaths[i] = texturePaths[i].Replace($"{AppearanceLoader.PathPrefix}/", string.Empty);

                // First, look for an appearance with a name, equal to actor's ID.
                if (texturePaths.Any(t => t.EqualsFast(Id)))
                    return texturePaths.First(t => t.EqualsFast(Id));

                // Then, try a `Default` appearance.
                if (texturePaths.Any(t => t.EqualsFast("Default")))
                    return texturePaths.First(t => t.EqualsFast("Default"));

                // Finally, fallback to a first defined appearance.
                return texturePaths.FirstOrDefault();
            }

            return null;
        }

        protected virtual void ApplyTextureSettings (Texture2D texture)
        {
            if (texture && texture.wrapMode != TextureWrapMode.Clamp)
                texture.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
