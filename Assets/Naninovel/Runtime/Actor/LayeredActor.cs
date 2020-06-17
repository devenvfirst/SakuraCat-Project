// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <see cref="LayeredActorBehaviour"/> to represent the actor.
    /// </summary>
    public abstract class LayeredActor : MonoBehaviourActor
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected readonly TransitionalSpriteRenderer SpriteRenderer;
        protected LayeredActorBehaviour Behaviour { get; private set; }

        private readonly OrthoActorMetadata metadata;
        private readonly HashSet<string> heldAppearances = new HashSet<string>();
        private LocalizableResourceLoader<GameObject> prefabLoader;
        private RenderTexture appearanceTexture;
        private string defaultAppearance;
        private string appearance;
        private bool visible;

        public LayeredActor (string id, OrthoActorMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;

            SpriteRenderer = GameObject.AddComponent<TransitionalSpriteRenderer>();
            SpriteRenderer.Pivot = metadata.Pivot;
            SpriteRenderer.PixelsPerUnit = metadata.PixelsPerUnit;
            SpriteRenderer.DepthPassEnabled = metadata.EnableDepthPass;
            SpriteRenderer.DepthAlphaCutoff = metadata.DepthAlphaCutoff;
            SpriteRenderer.CustomShader = metadata.CustomShader;

            SetVisibility(false);
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerMngr = Engine.GetService<IResourceProviderManager>();
            var localeMngr = Engine.GetService<ILocalizationManager>();
            prefabLoader = metadata.Loader.CreateLocalizableFor<GameObject>(providerMngr, localeMngr);

            var prefabResource = await prefabLoader.LoadAsync(Id);
            Behaviour = Engine.Instantiate(prefabResource.Object).GetComponent<LayeredActorBehaviour>();
            Behaviour.gameObject.name = prefabResource.Object.name;
            Behaviour.transform.SetParent(Transform);
            defaultAppearance = Behaviour.Composition;
        }

        public override async UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance))
                appearance = defaultAppearance;

            if (transition.HasValue) SpriteRenderer.Transition = transition.Value;

            Behaviour.ApplyComposition(appearance);
            var newRenderTexture = Behaviour.Render(metadata.PixelsPerUnit);
            await SpriteRenderer.TransitionToAsync(newRenderTexture, duration, easingType, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            // Release texture with the old appearance.
            if (ObjectUtils.IsValid(appearanceTexture))
                RenderTexture.ReleaseTemporary(appearanceTexture);
            appearanceTexture = newRenderTexture;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            // When revealing the actor and never rendered before — force render with default appearance.
            if (!Visible && visible && string.IsNullOrEmpty(appearance))
                SetAppearance(defaultAppearance);

            this.visible = visible;

            await SpriteRenderer.FadeToAsync(visible ? TintColor.a : 0, duration, easingType, cancellationToken);
        }

        public override async UniTask HoldResourcesAsync (object holder, string appearance)
        {
            if (heldAppearances.Count == 0)
            {
                var prefabResource = await prefabLoader.LoadAsync(Id);
                if (prefabResource.IsValid)
                    prefabResource.Hold(holder);
            }

            heldAppearances.Add(appearance);
        }

        public override void ReleaseResources (object holder, string appearance)
        {
            heldAppearances.Remove(appearance);

            if (heldAppearances.Count == 0)
                prefabLoader.GetLoadedOrNull(Id)?.Release(holder);
        }

        public override void Dispose ()
        {
            if (ObjectUtils.IsValid(appearanceTexture))
                RenderTexture.ReleaseTemporary(appearanceTexture);

            base.Dispose();

            prefabLoader?.UnloadAll();
        }

        protected virtual void SetAppearance (string appearance)
        {
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance))
                appearance = defaultAppearance;

            Behaviour.ApplyComposition(appearance);
            var newRenderTexture = Behaviour.Render(metadata.PixelsPerUnit);
            SpriteRenderer.MainTexture = newRenderTexture;

            // Release texture with the old appearance.
            if (ObjectUtils.IsValid(appearanceTexture))
                RenderTexture.ReleaseTemporary(appearanceTexture);
            appearanceTexture = newRenderTexture;
        }

        protected virtual void SetVisibility (bool visible)
        {
            // When revealing the actor and never rendered before — force render with default appearance.
            if (!Visible && visible && string.IsNullOrEmpty(appearance))
                SetAppearance(defaultAppearance);

            this.visible = visible;

            SpriteRenderer.Opacity = visible ? TintColor.a : 0;
        }

        protected override Color GetBehaviourTintColor () => SpriteRenderer.TintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            if (!Visible) // Handle visibility-controlled alpha of the tint color.
                tintColor.a = SpriteRenderer.TintColor.a;
            SpriteRenderer.TintColor = tintColor;
        }
    }
}
