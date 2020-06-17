// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel
{
    public class CGGalleryGridSlot : ScriptableGridSlot
    {
        public class Constructor : Constructor<CGGalleryGridSlot>
        {
            public Constructor (CGGalleryGridSlot prototype, string unlockableId, string textureLocalPath, 
                LocalizableResourceLoader<Texture2D> cgTextureLoader, OnClicked onClicked) : base(prototype, unlockableId, onClicked)
            {
                ConstructedSlot.textureLoader = cgTextureLoader;
                ConstructedSlot.textureLocalPath = textureLocalPath;
                ConstructedSlot.thumbnailImage.texture = ConstructedSlot.loadingTexture;
            }
        }

        public virtual string UnlockableId => Id;

        protected RawImage ThumbnailImage => thumbnailImage;
        protected Texture2D LockedTexture => lockedTexture;
        protected Texture2D LoadingTexture => loadingTexture;

        [SerializeField] private RawImage thumbnailImage = null;
        [SerializeField] private Texture2D lockedTexture = default;
        [SerializeField] private Texture2D loadingTexture = default;

        private string textureLocalPath;
        private IUnlockableManager unlockableManager;
        private LocalizableResourceLoader<Texture2D> textureLoader;

        public virtual async UniTask<Texture2D> LoadCGTextureAsync ()
        {
            Texture2D cgTexture;

            if (textureLoader.IsLoaded(textureLocalPath))
                cgTexture = textureLoader.GetLoadedOrNull(textureLocalPath);
            else
            {
                thumbnailImage.texture = loadingTexture;
                var resource = await textureLoader.LoadAsync(textureLocalPath);
                resource?.Hold(this);
                cgTexture = resource;
            }

            thumbnailImage.texture = unlockableManager.ItemUnlocked(UnlockableId) ? cgTexture : lockedTexture;

            return cgTexture;
        }

        public virtual void UnloadCGTexture ()
        {
            var loadedResource = textureLoader.GetLoadedOrNull(textureLocalPath);
            if (loadedResource?.IsValid ?? false)
                loadedResource.Release(this);
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(thumbnailImage, lockedTexture);

            unlockableManager = Engine.GetService<IUnlockableManager>();
        }
    }
}
