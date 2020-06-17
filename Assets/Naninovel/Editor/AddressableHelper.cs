// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEditor;

namespace Naninovel
{
    public static class AddressableHelper
    {
        #if ADDRESSABLES_AVAILABLE
        public static bool Available => true;
        #else
        public static bool Available => false;
        #endif

        #if ADDRESSABLES_AVAILABLE
        private static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings;
        #endif

        public static bool CheckAssetConflict (string assetGuid, string path, out string conflictAddress)
        {
            conflictAddress = null;
            #if ADDRESSABLES_AVAILABLE
            var entry = settings.FindAssetEntry(assetGuid);
            if (entry is null) return false;
            var address = PathToAddress(path);
            conflictAddress = entry.address;
            return entry.address != address;
            #else
            return false;
            #endif
        }

        public static void RemovePreviousEntries ()
        {
            #if ADDRESSABLES_AVAILABLE
            var group = settings.FindGroup(ResourceProviderConfiguration.AddressableId);
            if (group is null) return;
            settings.RemoveGroup(group);
            #endif
        }

        public static void CreateOrUpdateAddressableEntry (string assetGuid, string path)
        {
            #if ADDRESSABLES_AVAILABLE
            var address = PathToAddress(path);
            var label = ResourceProviderConfiguration.AddressableId;
            var groupName = ResourceProviderConfiguration.AddressableId;

            var group = FindOrCreateGroup(groupName);
            var entry = settings.CreateOrMoveEntry(assetGuid, group);

            entry.SetAddress(address);
            entry.SetLabel(label, true, true);

            EditorUtility.SetDirty(settings);
            #endif
        }

        public static void RebuildPlayerContent ()
        {
            #if ADDRESSABLES_AVAILABLE
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent();
            #endif
        }

        #if ADDRESSABLES_AVAILABLE
        [InitializeOnLoadMethod]
        private static void Initialize ()
        {
            EditorApplication.delayCall += HandleInitialize; // To prevent accessing the settings during editor recompile/setup (could create dublicate settings otherwise).
        }

        private static void HandleInitialize ()
        {
            // Also ensures the addressable settings are created.
            settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.GetSettings(true);
        }

        private static UnityEditor.AddressableAssets.Settings.AddressableAssetGroup FindOrCreateGroup (string groupName)
        {
            var group = settings.FindGroup(groupName);
            if (group is null) group = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
            return group;
        }

        private static string PathToAddress (string path) => PathUtils.Combine(ResourceProviderConfiguration.AddressableId, path);
        private static string AddressToPath (string address) => address.GetAfterFirst($"{ResourceProviderConfiguration.AddressableId}/");
        #endif
    }
}

