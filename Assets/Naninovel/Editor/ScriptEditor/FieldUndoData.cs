// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    [System.Serializable]
    public class FieldUndoData : ScriptableObject
    {
        [SerializeField] private List<string> fieldValues = new List<string>();

        private readonly List<LineTextField> fields = new List<LineTextField>();
        private bool updateSerializedObjectPending;
        private SerializedObject serializedObject;
        private SerializedProperty fieldValuesProperty;

        private void OnEnable ()
        {
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
        }

        private void OnDisable ()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
        }

        public void BindField (LineTextField field)
        {
            if (serializedObject is null)
            {
                serializedObject = new SerializedObject(this);
                fieldValuesProperty = serializedObject.FindProperty(nameof(fieldValues));
            }

            if (!fields.Contains(field))
            {
                fields.Add(field);
                fieldValues.Add(field.value);
                field.RegisterValueChangedCallback(HandleValueChanged);
            }
            else
            {
                var index = fields.IndexOf(field);
                fieldValues[index] = field.value;
            }

            if (!updateSerializedObjectPending) // For better performance on editor init.
            {
                EditorApplication.delayCall += UpdateSerializedObjectDelayed;
                updateSerializedObjectPending = true;
            }
            void UpdateSerializedObjectDelayed ()
            {
                serializedObject.UpdateIfRequiredOrScript();
                updateSerializedObjectPending = false;
            }
        }

        public void ResetBindings ()
        {
            fieldValues?.Clear();

            if (fields?.Count > 0)
            {
                fields.ForEach(f => f.UnregisterValueChangedCallback(HandleValueChanged));
                fields.Clear();
            }

            if (serializedObject != null)
            {
                serializedObject.Dispose();
                serializedObject = null;
            }
        }

        private void HandleValueChanged (ChangeEvent<string> evt)
        {
            if (serializedObject is null) return;
            if (evt.newValue == evt.previousValue) return;

            var index = fields.IndexOf(evt.currentTarget as LineTextField);
            fieldValuesProperty.GetArrayElementAtIndex(index).stringValue = fields[index].value;
            serializedObject.ApplyModifiedProperties();

            ScriptView.ScriptModified = true;
        }

        private void HandleUndoRedoPerformed ()
        {
            if (serializedObject is null) return;

            serializedObject.UpdateIfRequiredOrScript();

            for (int i = 0; i < fieldValuesProperty.arraySize; i++)
                fields[i].value = fieldValuesProperty.GetArrayElementAtIndex(i).stringValue;

            serializedObject.ApplyModifiedProperties();
        }
    }
}