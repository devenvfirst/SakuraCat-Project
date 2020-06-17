// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// The localizable loader will attempt to use <see cref="Naninovel.ILocalizationManager"/> to retrieve localized versions 
    /// of the requested resources and fallback to default loader behaviour when localized versions are not available.
    /// </summary>
    public class LocalizableResourceLoader<TResource> : ResourceLoader<TResource> 
        where TResource : UnityEngine.Object
    {
        protected readonly ILocalizationManager LocalizationManager;
        protected readonly List<Resource<TResource>> LoadedLocalizedResources;

        public LocalizableResourceLoader (List<IResourceProvider> providersList, ILocalizationManager localizationManager, string prefix = null)
            : base(providersList, prefix)
        {
            LocalizationManager = localizationManager;
            LoadedLocalizedResources = new List<Resource<TResource>>();
        }

        public override bool IsLoaded (string path, bool isFullPath = false)
        {
            if (!isFullPath) path = BuildFullPath(path);

            if (LocalizationManager != null && LocalizationManager.LocalizedResourceLoaded(path)) return true;

            return base.IsLoaded(path, true);
        }

        public override Resource<TResource> GetLoadedOrNull (string path, bool isFullPath = false)
        {
            if (!isFullPath) path = BuildFullPath(path);

            return LocalizationManager?.GetLoadedLocalizedResourceOrNull<TResource>(path) ?? base.GetLoadedOrNull(path, true);
        }

        public override async UniTask<Resource<TResource>> LoadAsync (string path, bool isFullPath = false)
        {
            if (!isFullPath) path = BuildFullPath(path);

            if (LocalizationManager is null || !await LocalizationManager.LocalizedResourceAvailableAsync<TResource>(path))
                return await base.LoadAsync(path, true);

            var localizedResource = await LocalizationManager.LoadLocalizedResourceAsync<TResource>(path);
            if (localizedResource != null && localizedResource.IsValid)
                LoadedLocalizedResources.Add(localizedResource);
            return localizedResource;
        }

        public override async UniTask<IEnumerable<Resource<TResource>>> LoadAllAsync (string path = null, bool isFullPath = false)
        {
            if (!isFullPath) path = BuildFullPath(path);

            if (LocalizationManager is null)
                return await base.LoadAllAsync(path, true);

            // 1. Locate all the original resources.
            var locatedResourcePaths = await base.LocateAsync(path, true);
            // 2. Load localized resources when available, original otherwise.
            return await UniTask.WhenAll(locatedResourcePaths.Select(p => LoadAsync(p, true)));
        }

        public override void Unload (string path, bool isFullPath = false)
        {
            if (!isFullPath) path = BuildFullPath(path);

            LocalizationManager?.UnloadLocalizedResource(path);
            LoadedLocalizedResources.RemoveAll(r => r is null || r.Path.EqualsFast(path));

            base.Unload(path, true);
        }

        /// <summary>
        /// Unloads all the resources (both localized and originals) previously loaded by this loader.
        /// </summary>
        public override void UnloadAll ()
        {
            foreach (var resource in LoadedLocalizedResources)
                LocalizationManager?.UnloadLocalizedResource(resource.Path);
            LoadedLocalizedResources.Clear();

            base.UnloadAll();
        }

        /// <summary>
        /// Retrieves all the resources (both localized and originals) loaded by this loader.
        /// </summary>
        public override List<Resource<TResource>> GetAllLoaded ()
        {
            var result = base.GetAllLoaded();
            result.AddRange(LoadedLocalizedResources.Where(r => r != null));
            return result;
        }
    }
}
