// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [CustomEditor(typeof(LayeredActorBehaviour), true)]
    public class LayeredActorBehaviourEditor : Editor
    {
        private const string mapFieldName = "compositionMap";

        private void OnEnable ()
        {
            EditorApplication.contextualPropertyMenu += HanlePropertyContextMenu;
        }

        private void OnDisable ()
        {
            EditorApplication.contextualPropertyMenu -= HanlePropertyContextMenu;
        }

        void HanlePropertyContextMenu (GenericMenu menu, SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Generic || 
                property.serializedObject.targetObject.GetType() != typeof(LayeredActorBehaviour) ||
                !property.propertyPath.Contains($"{mapFieldName}.Array.data[")) return;

            var propertyCopy = property.Copy();
            menu.AddItem(new GUIContent("Preview Composition"), false, () =>
            {
                var targetObj = propertyCopy.serializedObject.targetObject as LayeredActorBehaviour;
                var map = targetObj.GetType().GetField(mapFieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(targetObj) as List<LayeredActorBehaviour.CompositionMapItem>;
                var index = propertyCopy.propertyPath.GetAfterFirst($"{mapFieldName}.Array.data[").GetBefore("]").AsInvariantInt();
                var mapItem = map[index.Value];
                targetObj.ApplyComposition(mapItem.Composition);
                EditorUtility.SetDirty(propertyCopy.serializedObject.targetObject);
            });
        }
    }
}
