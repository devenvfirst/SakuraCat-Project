// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public static class GUIStyles
    {
        public static readonly GUIStyle NavigationButton;
        public static readonly GUIStyle IconButton;
        public static readonly GUIStyle TagIcon;
        public static readonly GUIStyle ScriptLabelTag;
        public static readonly GUIStyle ScriptGotoTag;
        public static readonly GUIStyle RichLabelStyle;

        static GUIStyles ()
        {
            NavigationButton = new GUIStyle("AC Button");
            NavigationButton.stretchWidth = true;
            NavigationButton.fixedWidth = 0;

            IconButton = GetStyle("IconButton");
            TagIcon = GetStyle("AssetLabel Icon");
            ScriptLabelTag = GetStyle("AssetLabel");

            var scriptGotoTexture = Resources.Load<Texture2D>("Naninovel/ScriptGotoIcon");
            ScriptGotoTag = new GUIStyle(ScriptLabelTag);
            ScriptGotoTag.normal.background = scriptGotoTexture;

            RichLabelStyle = new GUIStyle(GUI.skin.label);
            RichLabelStyle.richText = true;
        }

        private static GUIStyle GetStyle (string styleName)
        {
            var style = GUI.skin.FindStyle(styleName) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (style is null) Debug.LogError($"Missing built-in guistyle `{styleName}`.");
            return style;
        }
    }
}
