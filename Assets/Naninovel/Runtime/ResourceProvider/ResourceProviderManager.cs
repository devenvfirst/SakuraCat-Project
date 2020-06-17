// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IResourceProviderManager"/>
    [InitializeAtRuntime]
    public class ResourceProviderManager : IResourceProviderManager
    {
        public event Action<string> OnProviderMessage;

        public ResourceProviderConfiguration Configuration { get; }

        private readonly Dictionary<string, IResourceProvider> providers = new Dictionary<string, IResourceProvider>();

        public ResourceProviderManager (ResourceProviderConfiguration config)
        {
            Configuration = config;

            if (config.ResourcePolicy == ResourcePolicy.Dynamic && config.OptimizeLoadingPriority)
                Application.backgroundLoadingPriority = ThreadPriority.Low;
        }

        public UniTask InitializeServiceAsync ()
        {
            if (ResourceProviderConfiguration.EditorProvider != null)
                ResourceProviderConfiguration.EditorProvider.OnMessage += (message) => HandleProviderMessage(ResourceProviderConfiguration.EditorProvider, message);

            Application.lowMemory += HandleLowMemoryAsync;
            return UniTask.CompletedTask;
        }

        public void ResetService () { }

        public void DestroyService ()
        {
            Application.lowMemory -= HandleLowMemoryAsync;
            foreach (var provider in providers.Values)
                provider?.UnloadResources();
            ResourceProviderConfiguration.EditorProvider?.UnloadResources();
        }

        public bool ProviderInitialized (string providerType) => providers.ContainsKey(providerType);

        public IResourceProvider GetProvider (string providerType)
        {
            if (!providers.ContainsKey(providerType))
                providers[providerType] = InitializeProvider(providerType);
            return providers[providerType];
        }

        public List<IResourceProvider> GetProviders (List<string> providerTypes)
        {
            var result = new List<IResourceProvider>();

            // Include editor provider if assigned.
            if (ResourceProviderConfiguration.EditorProvider != null)
                result.Add(ResourceProviderConfiguration.EditorProvider);

            // Include requested providers in order.
            foreach (var providerType in providerTypes.Distinct())
            {
                var provider = GetProvider(providerType);
                if (provider != null) result.Add(provider);
            }

            return result;
        }

        private IResourceProvider InitializeProjectProvider ()
        {
            var projectProvider = new ProjectResourceProvider(Configuration.ProjectRootPath);
            return projectProvider;
        }

        private IResourceProvider InitializeGoogleDriveProvider ()
        {
            #if UNITY_GOOGLE_DRIVE_AVAILABLE
            var gDriveProvider = new GoogleDriveResourceProvider(Configuration.GoogleDriveRootPath, Configuration.GoogleDriveCachingPolicy, Configuration.GoogleDriveRequestLimit);
            gDriveProvider.AddConverter(new JpgOrPngToTextureConverter());
            gDriveProvider.AddConverter(new GDocToScriptAssetConverter());
            gDriveProvider.AddConverter(new Mp3ToAudioClipConverter());
            return gDriveProvider;
            #else
            return null;
            #endif
        }

        private IResourceProvider InitializeLocalProvider ()
        {
            var localProvider = new LocalResourceProvider(Configuration.LocalRootPath);
            localProvider.AddConverter(new JpgOrPngToTextureConverter());
            localProvider.AddConverter(new NaniToScriptAssetConverter());
            localProvider.AddConverter(new WavToAudioClipConverter());
            localProvider.AddConverter(new Mp3ToAudioClipConverter());
            return localProvider;
        }

        private IResourceProvider InitializeAddresableProvider ()
        {
            #if ADDRESSABLES_AVAILABLE
            if (Application.isEditor && !Configuration.AllowAddressableInEditor) return null; // Otherwise could be issues with addressables added on previous build, but renamed after.
            var extraLabels = Configuration.ExtraLabels != null && Configuration.ExtraLabels.Length > 0 ? Configuration.ExtraLabels : null;
            return new AddressableResourceProvider(ResourceProviderConfiguration.AddressableId, extraLabels);
            #else
            return null;
            #endif
        }

        private IResourceProvider InitializeProvider (string providerType)
        {
            IResourceProvider provider;

            switch (providerType)
            {
                case ResourceProviderConfiguration.ProjectTypeName:
                    provider = InitializeProjectProvider();
                    break;
                case ResourceProviderConfiguration.AddressableTypeName:
                    provider = InitializeAddresableProvider();
                    break;
                case ResourceProviderConfiguration.LocalTypeName:
                    provider = InitializeLocalProvider();
                    break;
                case ResourceProviderConfiguration.GoogleDriveTypeName:
                    provider = InitializeGoogleDriveProvider();
                    break;
                default:
                    var customType = Type.GetType(providerType);
                    if (customType is null)
                    {
                        Debug.LogError($"Failed to initialize '{providerType}' resource provider. Make sure provider types are set correctly in `Loader` properties of the Naninovel configuration menus.");
                        return null;
                    }
                    provider = (IResourceProvider)Activator.CreateInstance(customType);
                    if (provider is null) Debug.LogError($"Failed to initialize '{providerType}' custom resource provider. Make sure the implementation has a parameterless constructor.");
                    return provider;
            }

            if (provider != null)
                provider.OnMessage += (message) => HandleProviderMessage(provider, message);

            return provider;
        }

        private void HandleProviderMessage (IResourceProvider provider, string message)
        {
            OnProviderMessage?.Invoke($"[{provider.GetType().Name}] {message}");
        }

        private async void HandleLowMemoryAsync ()
        {
            Debug.LogWarning("Forcing resource unloading due to out of memory.");
            await Resources.UnloadUnusedAssets();
        }
    } 
}
