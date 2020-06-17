// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CGGalleryPanel : CustomUI, ICGGalleryUI
    {
        public int CGCount => grid.SlotCount;

        protected string UnlockableIdPrefix => unlockableIdPrefix;
        protected ResourceLoaderConfiguration[] CGSources => cgSources;
        protected ScriptableButton ViewerPanel => viewerPanel;
        protected RawImage ViewerImage => viewerImage;
        protected CGGalleryGrid Grid => grid;

        [Header("CG Setup")]
        [Tooltip("All the unlockable item IDs with the specified prefix will be considered CG items.")]
        [SerializeField] private string unlockableIdPrefix = "CG";
        [Tooltip("The spcified resource loaders will be used to retrieve the available CG slots and associated textures.")]
        [SerializeField] private ResourceLoaderConfiguration[] cgSources = new[] {
            new ResourceLoaderConfiguration { PathPrefix = $"{UnlockablesConfiguration.DefaultUnlockablesPathPrefix}/CG" },
            new ResourceLoaderConfiguration { PathPrefix = $"{BackgroundsConfiguration.DefaultPathPrefix}/{BackgroundsConfiguration.MainActorId}/CG" },
        };

        [Header("UI Setup")]
        [SerializeField] private ScriptableButton viewerPanel = default;
        [SerializeField] private RawImage viewerImage = default;
        [SerializeField] private CGGalleryGrid grid = default;

        private IUnlockableManager unlockableManager;
        private IResourceProviderManager providerManager;
        private ILocalizationManager localizationManager;
        private IInputManager inputManager;

        public override async UniTask InitializeAsync ()
        {
            foreach (var loaderConfig in cgSources)
            {
                // 1. Locate all the available textures under the source path.
                var loader = loaderConfig.CreateLocalizableFor<Texture2D>(providerManager, localizationManager);
                var resourcePaths = await loader.LocateAsync(string.Empty);
                // 2. Iterate the textures, adding them to the grid as CG slots.
                foreach (var resourcePath in resourcePaths)
                {
                    var textureLocalPath = loader.BuildLocalPath(resourcePath);
                    var unlockableId = $"{unlockableIdPrefix}/{textureLocalPath}";
                    if (grid.SlotExists(unlockableId)) continue;
                    grid.AddSlot(new CGGalleryGridSlot.Constructor(grid.SlotPrototype, unlockableId, textureLocalPath, loader, HandleSlotClicked).ConstructedSlot);
                }
            }
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            foreach (var slot in grid.GetAllSlots())
            {
                if (visible) slot.LoadCGTextureAsync().Forget();
                else slot.UnloadCGTexture();
            }
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(grid, viewerPanel, viewerImage);

            unlockableManager = Engine.GetService<IUnlockableManager>();
            providerManager = Engine.GetService<IResourceProviderManager>();
            localizationManager = Engine.GetService<ILocalizationManager>();
            inputManager = Engine.GetService<IInputManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            viewerPanel.OnButtonClicked += viewerPanel.Hide;

            if (inputManager?.GetCancel() != null)
                inputManager.GetCancel().OnStart += viewerPanel.Hide;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            viewerPanel.OnButtonClicked -= viewerPanel.Hide;

            if (inputManager?.GetCancel() != null)
                inputManager.GetCancel().OnStart -= viewerPanel.Hide;
        }

        protected virtual async void HandleSlotClicked (string id)
        {
            var slot = grid.GetSlot(id);
            if (!unlockableManager.ItemUnlocked(slot.UnlockableId)) return;

            var cgTexture = await slot.LoadCGTextureAsync();
            viewerImage.texture = cgTexture;
            viewerImage.SetMaterialDirty(); // Otherwise it won't show after closing CG panel and returning back (Unity regression).
            viewerPanel.Show();
        }
    }
}
