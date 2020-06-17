// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class EngineSettings : ConfigurationSettings<EngineConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(EngineConfiguration.CustomInitializationUI)] = property => { if (Configuration.ShowInitializationUI) EditorGUILayout.PropertyField(property); },
            [nameof(EngineConfiguration.ObjectsLayer)] = property => {
                if (!Configuration.OverrideObjectsLayer) return;
                var label = EditorGUI.BeginProperty(Rect.zero, null, property);
                property.intValue = EditorGUILayout.LayerField(label, property.intValue);
            },
            [nameof(EngineConfiguration.ToggleConsoleKey)] = property => { if (Configuration.EnableDevelopmentConsole) EditorGUILayout.PropertyField(property); }
        };
    }
}
