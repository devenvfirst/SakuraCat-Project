// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.UI;
using System;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IManagedUI"/> objects.
    /// </summary>
    public interface IUIManager : IEngineService<UIConfiguration>
    {
        /// <summary>
        /// Can be overriden to apply a system font of the set name to all the text elements contained in the managed UI.
        /// Null identifies that a default font is used.
        /// </summary>
        string Font { get; set; }
        /// <summary>
        /// Can be overriden to apply font size to all the text elements contained in the managed UI.
        /// Null identifies that a default font size is used.
        /// </summary>
        int? FontSize { get; set; }

        /// <summary>
        /// Instatiates provided prefab, initializes and adds <see cref="IManagedUI"/> component (should be on the root object of the prefab)
        /// to the managed objects; applies UI-related engine configuration and game settings.
        /// </summary>
        /// <param name="prefab">The prefab to spawn. Should have a <see cref="IManagedUI"/> component attached to the root object.</param>
        UniTask<IManagedUI> InstantiatePrefabAsync (GameObject prefab);
        /// <summary>
        /// Returns a managed UI of the provided type <typeparamref name="T"/>.
        /// Results per requested types are cached, so it's fine to use this method frequently.
        /// </summary>
        T GetUI<T> () where T : class, IManagedUI;
        /// <summary>
        /// Returns a managed UI of the provided type.
        /// Results per requested types are cached, so it's fine to use this method frequently.
        /// </summary>
        IManagedUI GetUI (Type type);
        /// <summary>
        /// Returns a managed UI based on the provided prefab name.
        /// </summary>
        IManagedUI GetUI (string prefabName);
        /// <summary>
        /// Applies provided render mode and camera for all the managed UI objects.
        /// </summary>
        void SetRenderMode (RenderMode renderMode, Camera renderCamera);
        /// <summary>
        /// Controls whether the UI (as a whole) is rendered (visible); won't affect visibility state of any particular UI.
        /// Will also spawn <see cref="ClickThroughPanel"/>, which will block input to prevent user from re-showing the UI,
        /// unless <paramref name="allowToggle"/> is true, in which case it'll be possible to re-show the UI with hotkeys and by clicking anywhere on the screen.
        /// </summary>
        void SetUIVisibleWithToggle (bool visible, bool allowToggle = true);
        /// <summary>
        /// Sets provided UI as modal, disabling interaction with all the other UIs.
        /// Provide null to restore interaction with the affected UIs.
        /// </summary>
        void SetModalUI (IManagedUI modalUI);
    }
}
