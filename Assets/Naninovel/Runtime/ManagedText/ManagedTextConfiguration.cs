// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class ManagedTextConfiguration : Configuration
    {
        public const string DefaultManagedTextPathPrefix = "Text";

        [Tooltip("Configuration of the resource loader used with the managed text documents.")]
        public ResourceLoaderConfiguration Loader = new ResourceLoaderConfiguration { PathPrefix = DefaultManagedTextPathPrefix };
    }
}
