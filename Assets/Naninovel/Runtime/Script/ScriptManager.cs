// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptManager"/>
    [InitializeAtRuntime]
    public class ScriptManager : IScriptManager
    {
        public event Action OnScriptLoadStarted;
        public event Action OnScriptLoadCompleted;

        public ScriptsConfiguration Configuration { get; }
        public string StartGameScriptName { get; private set; }
        public bool CommunityModdingEnabled => Configuration.EnableCommunityModding;
        public UI.ScriptNavigatorPanel ScriptNavigator { get; private set; }
        public int TotalCommandsCount { get; private set; }

        private const string navigatorPrefabResourcesPath = "Naninovel/ScriptNavigator";

        private readonly IResourceProviderManager providerManager;
        private readonly ILocalizationManager localizationManager;
        private readonly Dictionary<string, Script> localizationScripts;
        private ResourceLoader<Script> scriptLoader, externalScriptLoader;

        public ScriptManager (ScriptsConfiguration config, IResourceProviderManager providerManager, ILocalizationManager localizationManager)
        {
            Configuration = config;
            this.providerManager = providerManager;
            this.localizationManager = localizationManager;
            localizationScripts = new Dictionary<string, Script>();
        }

        public async UniTask InitializeServiceAsync ()
        {
            scriptLoader = Configuration.Loader.CreateFor<Script>(providerManager);
            if (CommunityModdingEnabled)
                externalScriptLoader = Configuration.ExternalLoader.CreateFor<Script>(providerManager);

            if (Application.isPlaying && Configuration.EnableNavigator)
            {
                var navigatorPrefab = Resources.Load<UI.ScriptNavigatorPanel>(navigatorPrefabResourcesPath);
                ScriptNavigator = Engine.Instantiate(navigatorPrefab, "ScriptNavigator");
                ScriptNavigator.SortingOrder = Configuration.NavigatorSortOrder;
                ScriptNavigator.SetVisibility(false);
            }

            if (string.IsNullOrEmpty(Configuration.StartGameScript))
            {
                var scriptPaths = await scriptLoader.LocateAsync(string.Empty);
                StartGameScriptName = scriptPaths.FirstOrDefault()?.Replace(scriptLoader.PathPrefix + "/", string.Empty);
            }
            else StartGameScriptName = Configuration.StartGameScript;

            if (Configuration.CountTotalCommands)
                TotalCommandsCount = await CountTotalCommandsAsync();
        }

        public void ResetService () { }

        public void DestroyService ()
        {
            if (ScriptNavigator)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(ScriptNavigator.gameObject);
                else UnityEngine.Object.DestroyImmediate(ScriptNavigator.gameObject);
            }
        }

        public async UniTask<IEnumerable<Script>> LoadExternalScriptsAsync ()
        {
            if (!CommunityModdingEnabled)
                return new List<Script>();

            OnScriptLoadStarted?.Invoke();
            var scriptResources = await externalScriptLoader.LoadAllAsync();
            OnScriptLoadCompleted?.Invoke();
            return scriptResources.Select(r => r.Object);
        }

        public async UniTask<Script> LoadScriptAsync (string name)
        {
            OnScriptLoadStarted?.Invoke();

            if (scriptLoader.IsLoaded(name))
            {
                OnScriptLoadCompleted?.Invoke();
                return scriptLoader.GetLoadedOrNull(name);
            }

            var scriptResource = await scriptLoader.LoadAsync(name);

            await TryAddLocalizationScriptAsync(scriptResource);

            OnScriptLoadCompleted?.Invoke();
            return scriptResource;
        }

        public async UniTask<IEnumerable<Script>> LoadAllScriptsAsync ()
        {
            OnScriptLoadStarted?.Invoke();
            var scriptResources = await scriptLoader.LoadAllAsync();
            var scripts = scriptResources.Select(r => r.Object);

            await UniTask.WhenAll(scripts.Select(s => TryAddLocalizationScriptAsync(s)));

            if (ScriptNavigator)
                ScriptNavigator.GenerateScriptButtons(scripts);

            OnScriptLoadCompleted?.Invoke();
            return scripts;
        }

        public void UnloadScript (string name)
        {
            if (scriptLoader.IsLoaded(name))
                scriptLoader.Unload(name);
            if (localizationScripts.ContainsKey(name))
            {
                localizationManager?.UnloadLocalizedResource(scriptLoader.BuildFullPath(name));
                localizationScripts.Remove(name);
            }
        }

        public void UnloadAllScripts ()
        {
            scriptLoader.UnloadAll();
            foreach (var scriptName in localizationScripts.Keys)
                localizationManager?.UnloadLocalizedResource(scriptLoader.BuildFullPath(scriptName));
            localizationScripts.Clear();

            #if UNITY_GOOGLE_DRIVE_AVAILABLE
            // Delete cached scripts when using Google Drive resource provider.
            if (providerManager.ProviderInitialized(ResourceProviderConfiguration.GoogleDriveTypeName))
                (providerManager.GetProvider(ResourceProviderConfiguration.GoogleDriveTypeName) as GoogleDriveResourceProvider).PurgeCachedResources(Configuration.Loader.PathPrefix);
            #endif

            if (ScriptNavigator) ScriptNavigator.DestroyScriptButtons();
        }

        public async UniTask ReloadAllScriptsAsync ()
        {
            UnloadAllScripts();
            await LoadAllScriptsAsync();
        }

        public Script GetLocalizationScriptFor (Script script)
        {
            if (localizationManager.SourceLocaleSelected() || !localizationScripts.ContainsKey(script.Name)) return null;
            return localizationScripts[script.Name];
        }

        private async UniTask TryAddLocalizationScriptAsync (Script script)
        {
            if (script is null) return;
            var fullPath = scriptLoader.BuildFullPath(script.Name);
            if (await localizationManager.LocalizedResourceAvailableAsync<Script>(fullPath))
            {
                var localizationScript = await localizationManager.LoadLocalizedResourceAsync<Script>(fullPath);
                localizationScripts[script.Name] = localizationScript;
            }
        }

        private async UniTask<int> CountTotalCommandsAsync ()
        {
            var result = 0;

            var scripts = await LoadAllScriptsAsync();
            foreach (var script in scripts)
            {
                var playlist = new ScriptPlaylist(script.Name, script.ExtractCommands());
                result += playlist.Count;
            }

            return result;
        }
    }
}
