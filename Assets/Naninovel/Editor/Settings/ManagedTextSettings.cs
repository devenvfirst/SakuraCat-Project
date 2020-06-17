// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class ManagedTextSettings : ConfigurationSettings<ManagedTextConfiguration>
    {
        protected override string HelpUri => "guide/managed-text.html";

        protected override void DrawConfigurationEditor ()
        {
            DrawDefaultEditor();

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Managed Text Utility", GUIStyles.NavigationButton))
                ManagedTextWindow.OpenWindow();
        }
    }
}
