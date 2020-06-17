// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Naninovel;
using System.Linq;
using System.Reflection;
using System;
using System.IO;
using System.Text;

namespace Naninovel
{
    //public static class Upgrader
    //{
    //    //[MenuItem("Naninovel/Upgrade/v1.9.5-beta to v1.9.6-beta", true)]
    //    //private static bool ValidateUpgrade195To196 () => EngineVersion.LoadFromResources().Version == "v1.9.6-beta";
    //    [MenuItem("Naninovel/Upgrade/v1.9.5-beta to v1.9.6-beta")]
    //    private static void Upgrade195To196 ()
    //    {
    //        if (!EditorUtility.DisplayDialog("Perform upgrade?",
    //            "Are you sure you want to perform v1.9.5-v1.9.6 upgrade? Configuration assets will be modified. Make sure to perform a backup before confirming.",
    //            "Upgrade", "Cancel")) return;

    //        // Remove `Naninovel/` path prefixes from config and metadata.
    //        var configTypes = ReflectionUtils.ExportedDomainTypes
    //            .Where(type => typeof(Configuration).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract);
    //        foreach (var configType in configTypes)
    //        {
    //            var configAsset = ProjectConfigurationProvider.LoadOrDefault(configType);

    //            // Root loaders.
    //            var rootLoadersInfo = configAsset.GetType().GetFields()
    //                .Where(f => f.FieldType == typeof(ResourceLoaderConfiguration)).ToList();
    //            foreach (var rootLoaderInfo in rootLoadersInfo)
    //                ProcessLoader(rootLoaderInfo, rootLoaderInfo.GetValue(configAsset));

    //            // Default metadata.
    //            var defaultMatadataPropertyInfo = configAsset.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
    //                .Where(p => p.Name == "DefaultActorMetadata").FirstOrDefault();
    //            if (defaultMatadataPropertyInfo != null)
    //            {
    //                var meta = defaultMatadataPropertyInfo.GetValue(configAsset) as ActorMetadata;
    //                var loaderFieldInfo = meta.GetType().GetField(nameof(ActorMetadata.Loader));
    //                ProcessLoader(loaderFieldInfo, meta.Loader);
    //            }

    //            // Actor metadata.
    //            var actorMatadataMapPropertyInfo = configAsset.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
    //                .Where(p => p.Name == "ActorMetadataMap").FirstOrDefault();
    //            if (actorMatadataMapPropertyInfo != null)
    //            {
    //                var actorMatadataMap = actorMatadataMapPropertyInfo.GetValue(configAsset);
    //                var metasFieldInfo = actorMatadataMap.GetType().GetFieldWithInheritence("metas", BindingFlags.NonPublic | BindingFlags.Instance);
    //                var metas = metasFieldInfo.GetValue(actorMatadataMap) as Array;
    //                for (int i = 0; i < metas.Length; i++)
    //                {
    //                    var meta = metas.GetValue(i) as ActorMetadata;
    //                    var loaderFieldInfo = meta.GetType().GetField(nameof(ActorMetadata.Loader));
    //                    ProcessLoader(loaderFieldInfo, meta.Loader);
    //                }
    //            }

    //            void ProcessLoader (FieldInfo loaderFieldInfo, object loaderObject)
    //            {
    //                var prefixFieldInfo = loaderFieldInfo.FieldType.GetField(nameof(ResourceLoaderConfiguration.PathPrefix));
    //                var currentValue = prefixFieldInfo.GetValue(loaderObject) as string;
    //                if (!currentValue.Contains("Naninovel/")) return;
    //                var newValue = currentValue.GetAfter("Naninovel/");
    //                prefixFieldInfo.SetValue(loaderObject, newValue);
    //            }

    //            EditorUtility.SetDirty(configAsset);
    //        }

    //        // Remove `Naninovel/` path prefixes from editor resources.
    //        var editorResources = EditorResources.LoadOrDefault();
    //        var editorResourcesPath = AssetDatabase.GetAssetPath(editorResources);
    //        var editorResourcesText = File.ReadAllText(editorResourcesPath, Encoding.UTF8);
    //        editorResourcesText = editorResourcesText.Replace("Naninovel/", string.Empty);
    //        File.WriteAllText(editorResourcesPath, editorResourcesText, Encoding.UTF8);
    //        AssetDatabase.ImportAsset(editorResourcesPath, ImportAssetOptions.ForceUpdate);

    //        AssetDatabase.Refresh();
    //        AssetDatabase.SaveAssets();
    //    }

    //    [MenuItem("Naninovel/Upgrade/v1.9.8-beta to v1.10")]
    //    private static void Upgrade198To110 ()
    //    {
    //        if (!EditorUtility.DisplayDialog("Perform upgrade?",
    //           "Are you sure you want to perform v1.9.8-v1.10 upgrade? Configuration assets will be modified. Make sure to perform a backup before confirming.",
    //           "Upgrade", "Cancel")) return;

    //        var stateConfig = ProjectConfigurationProvider.LoadOrDefault<StateConfiguration>();
    //        stateConfig.GameStateHandler = typeof(IOGameStateSlotManager).AssemblyQualifiedName;
    //        stateConfig.GlobalStateHandler = typeof(IOGlobalStateSlotManager).AssemblyQualifiedName;
    //        stateConfig.SettingsStateHandler = typeof(IOSettingsSlotManager).AssemblyQualifiedName;

    //        EditorUtility.SetDirty(stateConfig);
    //        AssetDatabase.Refresh();
    //        AssetDatabase.SaveAssets();
    //    }
    //}
}
