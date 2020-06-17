// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [CustomPropertyDrawer(typeof(ScenePositionAttribute))]
    public class ScenePositionPropertyDrawer : PropertyDrawer
    {
        private enum PositionModeType { World, Scene }

        private static readonly GUIContent positionModeContent = new GUIContent("Position Mode", "You can choose to specify position in world-space units (as seen in Transfrom component of game objects) or in Naninovel scene space relative to current reference resolution set in camera config, where `0,0` is the bottom left, `50,50` is the center and `100,100` is the top right corner of the screen (as set in `pos` parameters of some script commands).");
        private static readonly GUIContent scenePositionContent = new GUIContent("Scene Position");

        private readonly CameraConfiguration cameraConfiguration;
        private PositionModeType positionMode = PositionModeType.Scene;

        public ScenePositionPropertyDrawer ()
        {
            cameraConfiguration = ProjectConfigurationProvider.LoadOrDefault<CameraConfiguration>();
        } 

        public override void OnGUI (Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            positionMode = (PositionModeType)EditorGUI.EnumPopup(rect, positionModeContent, positionMode);

            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (positionMode == PositionModeType.Scene)
            {
                var scenePosition = cameraConfiguration.WorldToSceneSpace(property.vector3Value);
                scenePosition.x *= 100;
                scenePosition.y *= 100;
                EditorGUI.BeginChangeCheck();
                scenePosition = EditorGUI.Vector3Field(rect, scenePositionContent, scenePosition);
                if (EditorGUI.EndChangeCheck())
                {
                    scenePosition.x /= 100;
                    scenePosition.y /= 100;
                    property.vector3Value = cameraConfiguration.SceneToWorldSpace(scenePosition);
                }
            }
            else EditorGUI.PropertyField(rect, property);
        }

        public override float GetPropertyHeight (SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight * 2) + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
