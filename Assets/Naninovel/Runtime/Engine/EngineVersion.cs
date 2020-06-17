// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Stores engine version and build number.
    /// </summary>
    public class EngineVersion : ScriptableObject
    {
        /// <summary>
        /// Version identifier of the engine release.
        /// </summary>
        public string Version => engineVersion;
        /// <summary>
        /// Date and time the release was built.
        /// </summary>
        public string Build => buildDate;

        private const string resourcesPath = "Naninovel/" + nameof(EngineVersion);

        [SerializeField, ReadOnly] private string engineVersion = string.Empty;
        [SerializeField, ReadOnly] private string buildDate = string.Empty;

        public static EngineVersion LoadFromResources ()
        {
            return Resources.Load<EngineVersion>(resourcesPath);
        }
    }
}
