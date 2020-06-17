// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    /// <remarks>
    /// On build pre-process: 
    ///   - When addresable provider is used: assign an addressable address and label to the assets referenced in <see cref="EditorResources"/>;
    ///   - Otherwise: copy the <see cref="EditorResources"/> assets to a temp `Resources` folder (except the assets already stored in `Resources` folders).
    /// On build post-process or build fail: 
    ///   - restore any affected assets and delete the created temporary `Resources` folder.
    /// </remarks>
    public static class BuildProcessor
    {
        public const string TempResourcesPath = "Assets/TEMP_NANINOVEL/Resources";
        public const string TempStreamingPath = "Assets/StreamingAssets";

        private static ResourceProviderConfiguration config;
        private static bool useAddressables;

        [InitializeOnLoadMethod]
        private static void Initialize ()
        {
            if (ProjectConfigurationProvider.LoadOrDefault<ResourceProviderConfiguration>().EnableBuildProcessing)
                BuildPlayerWindow.RegisterBuildPlayerHandler(BuildHandler);

            ProjectResourcesBuildProcessor.TempFolderPath = TempResourcesPath;
        }

        private static void BuildHandler (BuildPlayerOptions options)
        {
            try
            {
                PreprocessBuild(options);
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
                PostprocessBuild();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Build failed: {e}");
                AssetDatabase.DeleteAsset(TempResourcesPath.GetBeforeLast("/")); 
            }
        }

        public static void PreprocessBuild (BuildPlayerOptions options)
        {
            config = ProjectConfigurationProvider.LoadOrDefault<ResourceProviderConfiguration>();

            useAddressables = AddressableHelper.Available && config.UseAddressables;
            if (!useAddressables) Debug.Log("Consider installing the Addressable Asset System (via Unity's package manager) and enabling `Use Addressables` in the Naninovel's `Resource Provider` configuration menu. When the system is not available, all the assets assigned as Naninovel resources and not stored in `Resources` folders will be copied and re-imported when building the player, which could significantly increase the build time.");

            if (useAddressables) AddressableHelper.RemovePreviousEntries();

            EditorUtils.CreateFolderAsset(TempResourcesPath);

            var records = EditorResources.LoadOrDefault().GetAllRecords();
            var projectResources = ProjectResources.Get();
            var progress = 0;
            foreach (var record in records)
            {
                progress++;

                var resourcePath = record.Key;
                var assetGuid = record.Value;
                var resourceAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                if (string.IsNullOrEmpty(resourceAssetPath) || !EditorUtils.AssetExistsByPath(resourceAssetPath))
                {
                    Debug.LogWarning($"Failed to resolve `{resourcePath}` asset path from GUID stored in `EditorResources` asset. The resource won't be included to the build.");
                    continue;
                }
                
                var resourceAsset = AssetDatabase.LoadAssetAtPath<Object>(resourceAssetPath);
                if (string.IsNullOrEmpty(resourceAssetPath))
                {
                    Debug.LogWarning($"Failed to load `{resourcePath}` asset. The resource won't be included to the build.");
                    continue;
                }

                if (EditorUtility.DisplayCancelableProgressBar("Processing Naninovel Resources", $"Processing '{resourceAssetPath}' asset...", progress / (float)records.Count))
                {
                    PostprocessBuild(); // Remove temporary assets.
                    throw new System.OperationCanceledException("Build was cancelled by the user.");
                }

                if (resourceAsset is SceneAsset)
                    ProcessSceneResource(resourcePath, resourceAsset as SceneAsset);
                else if (resourceAsset is UnityEngine.Video.VideoClip && options.target == BuildTarget.WebGL)
                    ProcessVideoResourceForWebGL(resourcePath, resourceAsset);
                else ProcessResourceAsset(assetGuid, resourcePath, resourceAsset, projectResources);
            }

            AssetDatabase.SaveAssets();

            if (useAddressables && config.AutoBuildBundles)
            {
                EditorUtility.DisplayProgressBar("Processing Naninovel Resources", "Building asset bundles...", 1f);
                AddressableHelper.RebuildPlayerContent();
            }

            EditorUtility.ClearProgressBar();
        }

        public static void PostprocessBuild ()
        {
            AssetDatabase.DeleteAsset(TempResourcesPath.GetBeforeLast("/"));
            AssetDatabase.SaveAssets();
        }

        private static void ProcessResourceAsset (string assetGuid, string path, Object asset, ProjectResources projectResources)
        {
            if (projectResources.ResourcePaths.Contains(path)) // Handle assets stored in `Resources`.
            {
                var otherAsset = Resources.Load(path, typeof(UnityEngine.Object)); // Check if a different asset is available under the same resources path.
                if (ObjectUtils.IsValid(otherAsset) && otherAsset != asset)
                {
                    var otherPath = AssetDatabase.GetAssetPath(otherAsset);
                    PostprocessBuild();
                    EditorUtility.ClearProgressBar();
                    throw new System.Exception($"Resource conflict detected: asset stored at `{otherPath}` conflicts with `{path}` Naninovel resource; rename or move the conflicting asset and rebuild the player.");
                }
                return;
            }

            if (useAddressables)
            {
                if (!AddressableHelper.CheckAssetConflict(assetGuid, path, out var conflictAddress))
                {
                    AddressableHelper.CreateOrUpdateAddressableEntry(assetGuid, path);
                    return;
                }
                Debug.Log($"Asset assigned as a Naninovel `{path}` resource is already registered in the Addressable Asset System as `{conflictAddress}`. It will be copied to prevent conflicts.");
            }

            var objPath = AssetDatabase.GetAssetPath(asset);
            var resourcePath = string.IsNullOrWhiteSpace(config.ProjectRootPath) ? PathUtils.Combine(TempResourcesPath, path) : PathUtils.Combine(TempResourcesPath, config.ProjectRootPath, path);
            if (objPath.Contains(".")) resourcePath += $".{objPath.GetAfter(".")}";

            EditorUtils.CreateFolderAsset(resourcePath.GetBeforeLast("/"));
            AssetDatabase.CopyAsset(objPath, resourcePath);
        }

        /// <summary>
        /// Make sure the scene is included to the build settings.
        /// </summary>
        private static void ProcessSceneResource (string path, SceneAsset sceneAsset)
        {
            var currentScenes = EditorBuildSettings.scenes.ToList();
            var scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            if (string.IsNullOrEmpty(scenePath) || currentScenes.Exists(s => s.path == scenePath)) return;

            currentScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = currentScenes.ToArray();
        }

        /// <summary>
        /// Copy video to `StreamingAssets` folder for streaming on WebGL (built-in videos are not supported on the platform).
        /// </summary>
        private static void ProcessVideoResourceForWebGL (string path, Object asset)
        {
            var objPath = AssetDatabase.GetAssetPath(asset);
            var streamingPath = PathUtils.Combine(TempStreamingPath, path);
            if (objPath.Contains(".")) streamingPath += $".{objPath.GetAfter(".")}";
            if (objPath.EndsWithFast(streamingPath))
                return; // The file is already in a streaming assets folder.
            EditorUtils.CreateFolderAsset(streamingPath.GetBeforeLast("/"));
            AssetDatabase.CopyAsset(objPath, streamingPath);
        }
    }
}
