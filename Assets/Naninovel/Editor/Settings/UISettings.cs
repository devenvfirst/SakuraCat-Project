// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class UISettings : ResourcefulSettings<UIConfiguration>
    {
        protected override string HelpUri => "guide/user-interface.html#ui-customization";

        protected override Type ResourcesTypeConstraint => typeof(GameObject);
        protected override string ResourcesCategoryId => Configuration.Loader.PathPrefix;
        protected override string ResourcesSelectionTooltip => "Use `@showUI %name%` to show and `@hideUI %name%` to hide the UI.";
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(UIConfiguration.ObjectsLayer)] = (property) => {
                var label = EditorGUI.BeginProperty(Rect.zero, null, property);
                property.intValue = EditorGUILayout.LayerField(label, property.intValue);
            }
        };

        [MenuItem("Naninovel/Resources/UI")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
