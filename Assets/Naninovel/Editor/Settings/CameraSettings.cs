// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class CameraSettings : ConfigurationSettings<CameraConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(CameraConfiguration.DefaultOrthoSize)] = property => { if (!Configuration.AutoCorrectOrthoSize) EditorGUILayout.PropertyField(property); },
            [nameof(CameraConfiguration.Orthographic)] = property => { if (!ObjectUtils.IsValid(Configuration.CustomCameraPrefab)) EditorGUILayout.PropertyField(property); },
            [nameof(CameraConfiguration.CustomUICameraPrefab)] = property => { if (Configuration.UseUICamera) EditorGUILayout.PropertyField(property); },
        };
    }
}
