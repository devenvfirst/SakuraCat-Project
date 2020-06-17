// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IUIManager"/>
    [InitializeAtRuntime]
    public class UIManager : IUIManager, IStatefulService<SettingsStateMap>
    {
        [Serializable]
        public class Settings
        {
            public string FontName = default;
            public int FontSize = -1;
        }

        private readonly struct ManagedUI
        {
            public readonly string Id;
            public readonly string PrefabName;
            public readonly GameObject GameObject;
            public readonly IManagedUI UIComponent;
            public readonly Type ComponentType;

            public ManagedUI (string prefabName, GameObject gameObject, IManagedUI uiComponent)
            {
                PrefabName = prefabName;
                GameObject = gameObject;
                UIComponent = uiComponent;
                ComponentType = UIComponent?.GetType();
                Id = $"{PrefabName}<{ComponentType.FullName}>";
            }
        }

        public UIConfiguration Configuration { get; }
        public string Font { get => ObjectUtils.IsValid(customFont) ? customFont.name : null; set => SetFont(value); }
        public int? FontSize { get => customFontSize; set => SetFontSize(value); }

        private const int defaultFontSize = 32;

        private readonly List<ManagedUI> managedUI = new List<ManagedUI>();
        private readonly Dictionary<Type, IManagedUI> cachedGetUIResults = new Dictionary<Type, IManagedUI>();
        private readonly Dictionary<IManagedUI, bool> modalState = new Dictionary<IManagedUI, bool>();
        private readonly ICameraManager cameraManager;
        private readonly IInputManager inputManager;
        private readonly IResourceProviderManager providersManager;
        private ResourceLoader<GameObject> loader;
        private Camera customCamera;
        private IInputSampler toggleUIInput;
        private Font customFont;
        private int? customFontSize;

        public UIManager (UIConfiguration config, IResourceProviderManager providersManager, ICameraManager cameraManager, IInputManager inputManager)
        {
            Configuration = config;
            this.providersManager = providersManager;
            this.cameraManager = cameraManager;
            this.inputManager = inputManager;

            // Instatiating the UIs after the engine itialization so that UIs can use Engine API in Awake() and OnEnable() methods.
            Engine.AddPostInitializationTask(InstantiateUIsAsync);
        }

        public UniTask InitializeServiceAsync ()
        {
            loader = Configuration.Loader.CreateFor<GameObject>(providersManager);

            toggleUIInput = inputManager.GetToggleUI();
            if (toggleUIInput != null)
                toggleUIInput.OnStart += ToggleUI;

            return UniTask.CompletedTask;
        }

        public void ResetService () { }

        public void DestroyService ()
        {
            if (toggleUIInput != null)
                toggleUIInput.OnStart -= ToggleUI;

            foreach (var managedUI in managedUI)
            {
                if (!ObjectUtils.IsValid(managedUI.GameObject)) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(managedUI.GameObject);
                else UnityEngine.Object.DestroyImmediate(managedUI.GameObject);
            }
            managedUI.Clear();
            cachedGetUIResults.Clear();

            loader.UnloadAll();

            Engine.RemovePostInitializationTask(InstantiateUIsAsync);
        }

        public void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                FontName = Font,
                FontSize = customFontSize ?? -1
            };
            stateMap.SetState(settings);
        }

        public UniTask LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings();
            Font = string.IsNullOrEmpty(settings.FontName) ? null : settings.FontName;
            FontSize = settings.FontSize <= 0 ? null : (int?)settings.FontSize;

            return UniTask.CompletedTask;
        }

        public async UniTask<IManagedUI> InstantiatePrefabAsync (GameObject prefab)
        {
            var gameObject = Engine.Instantiate(prefab, prefab.name, Configuration.ObjectsLayer);

            if (!gameObject.TryGetComponent<IManagedUI>(out var uiComponent))
            {
                Debug.LogError($"Failed to instatiate `{prefab.name}` UI prefab: the prefab doesn't contain a `CustomUI` or `IManagedUI` component on the root object.");
                return null;
            }

            uiComponent.SortingOrder += Configuration.SortingOffset;
            uiComponent.RenderMode = Configuration.RenderMode;
            uiComponent.RenderCamera = ObjectUtils.IsValid(customCamera) ? customCamera : ObjectUtils.IsValid(cameraManager.UICamera) ? cameraManager.UICamera : cameraManager.Camera;

            if (ObjectUtils.IsValid(customFont))
                uiComponent.SetFont(customFont);
            if (customFontSize.HasValue)
                uiComponent.SetFontSize(customFontSize.Value);

            var managedUI = new ManagedUI(prefab.name, gameObject, uiComponent);
            this.managedUI.Add(managedUI);

            await uiComponent.InitializeAsync();

            return uiComponent;
        }

        public T GetUI<T> () where T : class, IManagedUI => GetUI(typeof(T)) as T;

        public IManagedUI GetUI (Type type)
        {
            if (cachedGetUIResults.TryGetValue(type, out var cachedResult))
                return cachedResult;

            foreach (var managedUI in managedUI)
                if (type.IsAssignableFrom(managedUI.ComponentType))
                {
                    var result = managedUI.UIComponent;
                    cachedGetUIResults[type] = result;
                    return managedUI.UIComponent;
                }

            return null;
        }

        public IManagedUI GetUI (string prefabName)
        {
            foreach (var managedUI in managedUI)
                if (managedUI.PrefabName == prefabName)
                    return managedUI.UIComponent;
            return null;
        }

        public void SetRenderMode (RenderMode renderMode, Camera renderCamera)
        {
            customCamera = renderCamera;
            foreach (var managedUI in managedUI)
            {
                managedUI.UIComponent.RenderMode = renderMode;
                managedUI.UIComponent.RenderCamera = renderCamera;
            }
        }

        public void SetUIVisibleWithToggle (bool visible, bool allowToggle = true)
        {
            cameraManager.RenderUI = visible;

            var clickThroughPanel = GetUI<ClickThroughPanel>();
            if (ObjectUtils.IsValid(clickThroughPanel))
            {
                if (visible) clickThroughPanel.Hide();
                else
                {
                    if (allowToggle) clickThroughPanel.Show(true, ToggleUI, InputConfiguration.SubmitName, InputConfiguration.ToggleUIName, InputConfiguration.RollbackName);
                    else clickThroughPanel.Show(false, null, InputConfiguration.RollbackName);
                }
            }
        }

        public void SetModalUI (IManagedUI modalUI)
        {
            if (modalState.Count > 0) // Restore previous state.
            {
                foreach (var kv in modalState)
                    kv.Key.Interactable = kv.Value || (kv.Key is CustomUI customUI && customUI.ModalUI && customUI.Visible);
                modalState.Clear();
            }

            if (modalUI is null) return;

            foreach (var ui in managedUI)
            {
                modalState[ui.UIComponent] = ui.UIComponent.Interactable;
                ui.UIComponent.Interactable = false;
            }

            modalUI.Interactable = true;
        }

        private void SetFont (string fontName)
        {
            if ((string.IsNullOrEmpty(fontName) && !ObjectUtils.IsValid(customFont)) || Font == fontName) return;

            if (string.IsNullOrEmpty(fontName))
            {
                customFont = null;
                return;
            }

            customFont = UnityEngine.Font.CreateDynamicFontFromOSFont(fontName, defaultFontSize);
            if (!ObjectUtils.IsValid(customFont))
            {
                Debug.LogError($"Failed to create `{fontName}` font.");
                return;
            }

            foreach (var ui in managedUI)
                ui.UIComponent.SetFont(customFont);
        }

        private void SetFontSize (int? size)
        {
            if (customFontSize == size) return;

            customFontSize = size;

            if (size.HasValue)
                foreach (var ui in managedUI)
                    ui.UIComponent.SetFontSize(size.Value);
        }

        private void ToggleUI () => SetUIVisibleWithToggle(!cameraManager.RenderUI);

        private async UniTask InstantiateUIsAsync ()
        {
            var resources = await loader.LoadAllAsync();
            var tasks = resources.Select(r => InstantiatePrefabAsync(r));
            await UniTask.WhenAll(tasks);
        }
    }
}
