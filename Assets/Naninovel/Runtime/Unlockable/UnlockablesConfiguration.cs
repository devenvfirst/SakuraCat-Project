// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class UnlockablesConfiguration : Configuration
    {
        public const string DefaultUnlockablesPathPrefix = "Unlockables";

        [Tooltip("Configuration of the resource loader used with unlockable resources.")]
        public ResourceLoaderConfiguration Loader = new ResourceLoaderConfiguration { PathPrefix = DefaultUnlockablesPathPrefix };
    }
}
