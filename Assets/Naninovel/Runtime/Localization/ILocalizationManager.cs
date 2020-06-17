// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage the localization activities.
    /// </summary>
    public interface ILocalizationManager : IEngineService<LocalizationConfiguration>
    {
        /// <summary>
        /// Event invoked when the locale is changed.
        /// </summary>
        event Action<string> OnLocaleChanged;

        /// <summary>
        /// Language tag of the currently selected localization.
        /// </summary>
        string SelectedLocale { get; }

        /// <summary>
        /// Return language tags of the available localizations.
        /// </summary>
        IEnumerable<string> GetAvailableLocales ();
        /// <summary>
        /// Whether localization with the provided language tag is available.
        /// </summary>
        bool LocaleAvailable (string locale);
        /// <summary>
        /// Selects (switches to) localization with the provided language tag.
        /// </summary>
        UniTask SelectLocaleAsync (string locale);
        /// <summary>
        /// Adds an async delegate to invoke after changing a locale.
        /// </summary>
        void AddChangeLocaleTask (Func<UniTask> taskFunc);
        /// <summary>
        /// Removes an async delegate to invoke after changing a locale.
        /// </summary>
        void RemoveChangeLocaleTask (Func<UniTask> taskFunc);
        /// <summary>
        /// Checks whether a localized resource variant for a source resource with the provided 
        /// path is available under the currently selected localization.
        /// </summary>
        UniTask<bool> LocalizedResourceAvailableAsync<TResource> (string path) where TResource : UnityEngine.Object;
        /// <summary>
        /// Loads a localized resource variant for a source resource with the provided 
        /// path under the currently selected localization.
        /// </summary>
        UniTask<Resource<TResource>> LoadLocalizedResourceAsync<TResource> (string path) where TResource : UnityEngine.Object;
        /// <summary>
        /// Retrieves a localized resource variant for a source resource with the provided 
        /// path under the currently selected localization.
        /// </summary>
        Resource<TResource> GetLoadedLocalizedResourceOrNull<TResource> (string path) where TResource : UnityEngine.Object;
        /// <summary>
        /// Unloads previously loaded localized resource variant for a source resource with the provided path.
        /// </summary>
        void UnloadLocalizedResource (string path);
        /// <summary>
        /// Checks wither a localized resource variant for a source resource with the provided path is loaded.
        /// </summary>
        bool LocalizedResourceLoaded (string path);
    }
}
