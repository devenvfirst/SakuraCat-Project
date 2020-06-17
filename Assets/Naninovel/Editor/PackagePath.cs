// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides paths to various package-related folders and resources. All the returned paths are in absolute format.
    /// </summary>
    public static class PackagePath
    {
        public static string PackageRootPath => GetPackageRootPath();
        public static string PackageMarkerPath => Path.Combine(cachedPackageRootPath, markerSearchPattern); 
        public static string EditorResourcesPath => Path.Combine(PackageRootPath, "Editor/Resources/Naninovel");
        public static string RuntimeResourcesPath => Path.Combine(PackageRootPath, "Resources/Naninovel");
        public static string PrefabsPath => Path.Combine(PackageRootPath, "Prefabs");

        private const string markerSearchPattern = "Elringus.Naninovel.Editor.asmdef";
        private static string cachedPackageRootPath;

        private static string GetPackageRootPath ()
        {
            if (string.IsNullOrEmpty(cachedPackageRootPath) || !File.Exists(PackageMarkerPath))
            {
                var marker = Directory.GetFiles(Application.dataPath, markerSearchPattern, SearchOption.AllDirectories).FirstOrDefault();
                if (marker is null) { Debug.LogError($"Failed to find `{markerSearchPattern}` file."); return null; }
                cachedPackageRootPath = Directory.GetParent(marker)?.Parent?.FullName;
            }
            return cachedPackageRootPath;
        }
    }
}
