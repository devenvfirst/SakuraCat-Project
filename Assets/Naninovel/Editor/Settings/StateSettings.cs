// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class StateSettings : ConfigurationSettings<StateConfiguration>
    {
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(StateConfiguration.StateRollbackSteps)] = property => { if (Configuration.EnableStateRollback) EditorGUILayout.PropertyField(property); },
            [nameof(StateConfiguration.SavedRollbackSteps)] = property => { if (Configuration.EnableStateRollback) EditorGUILayout.PropertyField(property); },
            [nameof(StateConfiguration.GameStateHandler)] = property => { 
                EditorGUILayout.Space(); 
                EditorGUILayout.LabelField("Serialization Handlers", EditorStyles.boldLabel);
                DrawHandlersDropdown(property, gameHandlerImplementations, gameHandlerImplementationsLabels); 
            },
            [nameof(StateConfiguration.GlobalStateHandler)] = property => DrawHandlersDropdown(property, globalHandlerImplementations, globalHandlerImplementationsLabels),
            [nameof(StateConfiguration.SettingsStateHandler)] = property => DrawHandlersDropdown(property, settingsHandlerImplementations, settingsHandlerImplementationsLabels),
        };

        private static readonly string[] gameHandlerImplementations, gameHandlerImplementationsLabels;
        private static readonly string[] globalHandlerImplementations, globalHandlerImplementationsLabels;
        private static readonly string[] settingsHandlerImplementations, settingsHandlerImplementationsLabels;

        static StateSettings ()
        {
            InitializeHandlerOptions<ISaveSlotManager<GameStateMap>>(ref gameHandlerImplementations, ref gameHandlerImplementationsLabels);
            InitializeHandlerOptions<ISaveSlotManager<GlobalStateMap>>(ref globalHandlerImplementations, ref globalHandlerImplementationsLabels);
            InitializeHandlerOptions<ISaveSlotManager<SettingsStateMap>>(ref settingsHandlerImplementations, ref settingsHandlerImplementationsLabels);
        }

        private static void DrawHandlersDropdown (SerializedProperty property, string[] values, string[] labels)
        {
            var label = EditorGUI.BeginProperty(Rect.zero, null, property);
            var curIndex = ArrayUtility.IndexOf(values, property.stringValue ?? string.Empty);
            var newIndex = EditorGUILayout.Popup(label, curIndex, labels);
            property.stringValue = values.IsIndexValid(newIndex) ? values[newIndex] : string.Empty;
            EditorGUI.EndProperty();
        }

        private static void InitializeHandlerOptions<THandler> (ref string[] values, ref string[] labels) where THandler : ISaveSlotManager
        {
            values = ReflectionUtils.ExportedDomainTypes
                .Where(t => !t.IsAbstract && t.GetInterfaces().Contains(typeof(THandler)))
                .Select(t => t.AssemblyQualifiedName).ToArray();
            labels = values.Select(s => s.GetBefore(",")).ToArray();
        }
    }
}
