// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class InputSettings : ConfigurationSettings<InputConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(InputConfiguration.CustomEventSystem)] = property => { if (Configuration.SpawnEventSystem) EditorGUILayout.PropertyField(property); },
            [nameof(InputConfiguration.CustomInputModule)] = property => { if (Configuration.SpawnInputModule) EditorGUILayout.PropertyField(property); },
        };
    }
}
