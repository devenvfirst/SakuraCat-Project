// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEditor;
using UnityEditor.UI;

namespace Naninovel
{
    [CustomEditor(typeof(LabeledButton), true), CanEditMultipleObjects]
    public class LabeledButtonEditor : ButtonEditor
    {
        SerializedProperty labelTextProperty;
        SerializedProperty labelColorsProperty;

        protected override void OnEnable ()
        {
            base.OnEnable();

            labelTextProperty = serializedObject.FindProperty("labelText");
            labelColorsProperty = serializedObject.FindProperty("labelColors");
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.PropertyField(labelTextProperty);

            ++EditorGUI.indentLevel;
            {
                EditorGUILayout.PropertyField(labelColorsProperty);
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
