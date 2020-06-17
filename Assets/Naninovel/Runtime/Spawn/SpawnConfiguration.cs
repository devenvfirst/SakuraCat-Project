// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class SpawnConfiguration : Configuration
    {
        public const string DefaultPathPrefix = "Spawn";

        [Tooltip("Configuration of the resource loader used with spawn resources.")]
        public ResourceLoaderConfiguration Loader = new ResourceLoaderConfiguration { PathPrefix = DefaultPathPrefix };
    }
}
