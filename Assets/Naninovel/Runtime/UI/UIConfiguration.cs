// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class UIConfiguration : Configuration
    {
        public const string DefaultPathPrefix = "UI";

        [Tooltip("Configuration of the resource loader used with UI resources.")]
        public ResourceLoaderConfiguration Loader = new ResourceLoaderConfiguration { PathPrefix = DefaultPathPrefix };
        [Tooltip("The layer to assign for the UI elements instatiated by the engine. Used to cull the UI when using `toogle UI` feature.")]
        public int ObjectsLayer = 5;
        [Tooltip("The canvas render mode to apply for all the managed UI elements.")]
        public RenderMode RenderMode = RenderMode.ScreenSpaceCamera;
        [Tooltip("The sorting offset to apply for all the managed UI elements.")]
        public int SortingOffset = 1;
    }
}
