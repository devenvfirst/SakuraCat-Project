// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

#if SPRITE_DICING_AVAILABLE

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="SpriteDicing.DicedSpriteRenderer"/> to represent the actor.
    /// </summary>
    public class DicedSpriteCharacter : MonoBehaviourActor, ICharacterActor
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }
        public CharacterLookDirection LookDirection { get => GetLookDirection(); set => SetLookDirection(value); }

        protected readonly TransitionalSpriteRenderer SpriteRenderer;

        private readonly CharacterMetadata metadata;
        private readonly Material dicedMaterial;
        private readonly Mesh dicedMesh;
        private readonly HashSet<string> heldAppearances = new HashSet<string>();
        private LocalizableResourceLoader<SpriteDicing.DicedSpriteAtlas> atlasLoader;
        private RenderTexture appearanceTexture;
        private string appearance;
        private string defaultSpriteName;
        private bool visible;

        public DicedSpriteCharacter (string id, CharacterMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;

            SpriteRenderer = GameObject.AddComponent<TransitionalSpriteRenderer>();
            SpriteRenderer.Pivot = metadata.Pivot;
            SpriteRenderer.PixelsPerUnit = metadata.PixelsPerUnit;
            SpriteRenderer.DepthPassEnabled = metadata.EnableDepthPass;
            SpriteRenderer.DepthAlphaCutoff = metadata.DepthAlphaCutoff;
            SpriteRenderer.CustomShader = metadata.CustomShader;

            dicedMaterial = new Material(Shader.Find("Sprites/Default"));
            dicedMaterial.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

            dicedMesh = new Mesh();
            dicedMesh.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            dicedMesh.name = $"{id} Diced Sprite Mesh";

            SetVisibility(false);
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerMngr = Engine.GetService<IResourceProviderManager>();
            var localeMngr = Engine.GetService<ILocalizationManager>();
            atlasLoader = metadata.Loader.CreateLocalizableFor<SpriteDicing.DicedSpriteAtlas>(providerMngr, localeMngr);
        }

        public override async UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            var atlasResource = await atlasLoader.LoadAsync(Id);
            if (cancellationToken.IsCancellationRequested) return;
            if (!atlasResource.IsValid || atlasResource.Object.SpritesCount == 0) return;

            if (transition.HasValue) SpriteRenderer.Transition = transition.Value;

            if (string.IsNullOrEmpty(defaultSpriteName))
            {
                var sprites = atlasResource.Object.GetAllSprites();
                var defaultSprite = sprites.Find(s => s.Name.EndsWithFast("Default"));
                defaultSpriteName = ObjectUtils.IsValid(defaultSprite) ? defaultSprite.Name : sprites.First().Name;
            }

            if (string.IsNullOrEmpty(appearance))
                appearance = defaultSpriteName;

            this.appearance = appearance;

            // In case user stored source sprites in folders, the diced sprites will have dots in their names.
            var spriteName = appearance.Replace("/", ".");
            var dicedSprite = atlasResource.Object.GetSprite(spriteName);
            if (dicedSprite is null)
            {
                Debug.LogWarning($"Failed to get `{spriteName}` diced sprite from `{atlasResource.Object.name}` atlas to set `{appearance}` appearance for `{Id}` character.");
                return;
            }
            dicedSprite.FillMesh(dicedMesh);
            dicedMaterial.mainTexture = dicedSprite.AtlasTexture;

            // Create a texture with the new appearance.
            var spriteRect = dicedSprite.EvaluateSpriteRect();
            var newRenderTexture = RenderTexture.GetTemporary(Mathf.CeilToInt(spriteRect.width * metadata.PixelsPerUnit), Mathf.CeilToInt(spriteRect.height * metadata.PixelsPerUnit));
            Graphics.SetRenderTarget(newRenderTexture);
            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            var halfRectSize = spriteRect.size / 2f;
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(-halfRectSize.x, halfRectSize.x, -halfRectSize.y, halfRectSize.y, 0f, 100f));
            dicedMaterial.SetPass(0);
            var drawPos = new Vector3(spriteRect.width * dicedSprite.Pivot.x - halfRectSize.x, spriteRect.height * dicedSprite.Pivot.y - halfRectSize.y);
            Graphics.DrawMeshNow(dicedMesh, drawPos, Quaternion.identity);
            GL.PopMatrix();

            await SpriteRenderer.TransitionToAsync(newRenderTexture, duration, easingType, cancellationToken: cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;

            // Release texture with the old appearance.
            if (ObjectUtils.IsValid(appearanceTexture))
                RenderTexture.ReleaseTemporary(appearanceTexture);
            appearanceTexture = newRenderTexture;
        }

        public override async UniTask ChangeVisibilityAsync (bool isVisible, float duration, 
            EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            // When appearance is not set (and default one is not preloaded for some reason, eg when using dynamic parameters) 
            // and revealing the actor — attempt to set default appearance.
            if (!Visible && isVisible && string.IsNullOrWhiteSpace(Appearance))
                await ChangeAppearanceAsync(defaultSpriteName, 0, cancellationToken: cancellationToken);

            this.visible = isVisible;

            await SpriteRenderer.FadeToAsync(isVisible ? TintColor.a : 0, duration, easingType, cancellationToken);
        }

        public override async UniTask HoldResourcesAsync (object holder, string appearance)
        {
            if (heldAppearances.Count == 0)
            {
                var resource = await atlasLoader.LoadAsync(Id);
                if (resource.IsValid)
                    resource.Hold(holder);
            }

            heldAppearances.Add(appearance);
        }

        public override void ReleaseResources (object holder, string appearance)
        {
            heldAppearances.Remove(appearance);

            if (heldAppearances.Count == 0)
                atlasLoader.GetLoadedOrNull(Id)?.Release(holder);
        }

        public override void Dispose ()
        {
            if (ObjectUtils.IsValid(appearanceTexture))
                RenderTexture.ReleaseTemporary(appearanceTexture);

            base.Dispose();

            atlasLoader?.UnloadAll();
        }

        public UniTask ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration, 
            EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            SetLookDirection(lookDirection);
            return UniTask.CompletedTask;
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

        protected virtual void SetLookDirection (CharacterLookDirection lookDirection)
        {
            if (metadata.BakedLookDirection == CharacterLookDirection.Center) return;
            if (lookDirection == CharacterLookDirection.Center)
            {
                SpriteRenderer.FlipX = false;
                return;
            }
            if (lookDirection != LookDirection)
                SpriteRenderer.FlipX = !SpriteRenderer.FlipX;
        }

        protected virtual CharacterLookDirection GetLookDirection ()
        {
            switch (metadata.BakedLookDirection)
            {
                case CharacterLookDirection.Center:
                    return CharacterLookDirection.Center;
                case CharacterLookDirection.Left:
                    return SpriteRenderer.FlipX ? CharacterLookDirection.Right : CharacterLookDirection.Left;
                case CharacterLookDirection.Right:
                    return SpriteRenderer.FlipX ? CharacterLookDirection.Left : CharacterLookDirection.Right;
                default:
                    return default;
            }
        }
    }
}

#endif
