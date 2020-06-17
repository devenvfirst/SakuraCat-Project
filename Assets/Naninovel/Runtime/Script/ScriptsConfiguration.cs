// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    [System.Serializable]
    public class ScriptsConfiguration : Configuration
    {
        public enum GraphOrientationType { Vertical, Horizontal }

        public const string DefaultScriptsPathPrefix = "Scripts";

        [Tooltip("Configuration of the resource loader used with naninovel script resources.")]
        public ResourceLoaderConfiguration Loader = new ResourceLoaderConfiguration { PathPrefix = DefaultScriptsPathPrefix };
        [Tooltip("Name of the script to play right after the engine initialization.")]
        public string InitializationScript = default;
        [Tooltip("Name of the script to play when showing the Title UI. Can be used to setup the title screen scene (backgound, music, etc).")]
        public string TitleScript = default;
        [Tooltip("Name of the script to play when starting a new game. Will use first available when not provided.")]
        public string StartGameScript = default;
        [Tooltip("Whether to automatically add created naninovel scripts to the resources.")]
        public bool AutoAddScripts = true;
        [Tooltip("Whether to reload modified (both via visual and external editors) scripts and apply changes during playmode without restarting the playback.")]
        public bool HotReloadScripts = true;
        [Tooltip("Whether to calculate number of commands existing in all the available naninovel scripts on service initalization. If you don't use `TotalCommandsCount` property of a script manager and `CalculateProgress` function in naninovel script expressions, disable to reduce engine initalization time.")]
        public bool CountTotalCommands = false;

        [Header("Visual Editor")]
        [Tooltip("Whether to show visual script editor when a script is selected.")]
        public bool EnableVisualEditor = true;
        [Tooltip("Whether to hide un-assigned parameters of the command lines when the line is not hovered or focused.")]
        public bool HideUnusedParameters = true;
        [Tooltip("Hot key used to show `Insert Line` window when the visual editor is in focus. Set to `None` to disable.")]
        public KeyCode InsertLineKey = KeyCode.Space;
        [Tooltip("Modifier for the `Insert Line Key`. Set to `None` to disable.")]
        public EventModifiers InsertLineModifier = EventModifiers.Control;
        [Tooltip("Hot key used to save (serialize) the edited script when the visual editor is in focus. Set to `None` to disable.")]
        public KeyCode SaveScriptKey = KeyCode.S;
        [Tooltip("Modifier for the `Save Script Key`. Set to `None` to disable.")]
        public EventModifiers SaveScriptModifier = EventModifiers.Control;
        [Tooltip("How many script lines should be rendered per visual editor page.")]
        public int EditorPageLength = 1000;
        [Tooltip("Allows modifying default style of the visual editor.")]
        public StyleSheet EditorCustomStyleSheet = null;

        [Header("Script Graph")]
        [Tooltip("Whether to build the graph vertically or horizontally.")]
        public GraphOrientationType GraphOrientation = GraphOrientationType.Horizontal;
        [Tooltip("Padding to add for each node when performing auto align.")]
        public Vector2 GraphAutoAlignPadding = new Vector2(10, 0);
        [Tooltip("Allows modifying default style of the script graph.")]
        public StyleSheet GraphCustomStyleSheet = null;

        [Header("Community Modding")]
        [Tooltip("Whether to allow adding external naninovel scripts to the build.")]
        public bool EnableCommunityModding = false;
        [Tooltip("Configuration of the resource loader used with external naninovel script resources.")]
        public ResourceLoaderConfiguration ExternalLoader = new ResourceLoaderConfiguration {
            ProviderTypes = new List<string> { ResourceProviderConfiguration.LocalTypeName },
            PathPrefix = DefaultScriptsPathPrefix
        };

        [Header("Script Navigator")]
        [Tooltip("Whether to initializte script navigator to browse available naninovel scripts.")]
        public bool EnableNavigator = true;
        [Tooltip("Whether to show naninovel script navigator when script manager is initialized.")]
        public bool ShowNavigatorOnInit = false;
        [Tooltip("UI sort order of the script navigator.")]
        public int NavigatorSortOrder = 900;
    }
}
