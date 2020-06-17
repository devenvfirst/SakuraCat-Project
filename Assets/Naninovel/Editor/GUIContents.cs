// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class GUIContents
    {
        public static readonly GUIContent HelpIcon;

        private static readonly Type contentsType;

        static GUIContents ()
        {
            contentsType = typeof(EditorGUI).GetNestedType("GUIContents", BindingFlags.NonPublic);

            HelpIcon = contentsType.GetProperty("helpIcon", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as GUIContent;
        }
    }
}
