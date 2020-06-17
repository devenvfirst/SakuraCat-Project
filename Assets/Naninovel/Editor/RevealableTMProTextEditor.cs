// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

#if TMPRO_AVAILABLE

using TMPro.EditorUtilities;
using UnityEditor;

namespace Naninovel
{
    [CustomEditor(typeof(UI.RevealableTMProText), true)]
    [CanEditMultipleObjects]
    public class RevealableTMProTextEditor : TMP_UiEditorPanel
    {
        private SerializedProperty revealFadeWidth;
        private SerializedProperty slideClipRect;
        private SerializedProperty italicSlantAngle;
        private SerializedProperty rubyVerticalOffset;
        private SerializedProperty rubySizeScale;

        protected override void OnEnable ()
        {
            base.OnEnable();

            revealFadeWidth = serializedObject.FindProperty("revealFadeWidth");
            slideClipRect = serializedObject.FindProperty("slideClipRect");
            italicSlantAngle = serializedObject.FindProperty("italicSlantAngle");
            rubyVerticalOffset = serializedObject.FindProperty("rubyVerticalOffset");
            rubySizeScale = serializedObject.FindProperty("rubySizeScale");
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.LabelField("Revealing", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(revealFadeWidth);
                EditorGUILayout.PropertyField(slideClipRect);
                EditorGUILayout.PropertyField(italicSlantAngle);
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.LabelField("Ruby Text", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(rubyVerticalOffset);
                EditorGUILayout.PropertyField(rubySizeScale);
            }
            --EditorGUI.indentLevel;

            serializedObject.ApplyModifiedProperties();
        }
    } 
}

#endif