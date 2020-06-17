// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.UI
{
    public class CustomVariableGUI : MonoBehaviour
    {
        private class Record : IEquatable<Record>
        {
            public string Name = string.Empty, Value = string.Empty, EditedValue = string.Empty;
            public bool Changed => !Value.Equals(EditedValue, StringComparison.Ordinal);

            public Record (string name, string value)
            {
                Name = name;
                Value = EditedValue = value;
            }

            public override bool Equals (object obj) => obj is Record record && Equals(record);
            public bool Equals (Record other) => Name == other.Name;
            public override int GetHashCode () => 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
            public static bool operator == (Record left, Record right) => left.Equals(right);
            public static bool operator != (Record left, Record right) => !(left == right);
        }

        private const float width = 400;
        private const int windowId = 1;

        private static Rect windowRect = new Rect(Screen.width - width, 0, width, Screen.height * .85f);
        private static Vector2 scrollPos;
        private static bool show;
        private static string search;
        private static CustomVariableGUI instance;

        private readonly SortedList<string, Record> records = new SortedList<string, Record>();
        private ICustomVariableManager variableManager;
        private IStateManager stateManager;

        public static void Toggle ()
        {
            if (instance == null)
                instance = Engine.CreateObject<CustomVariableGUI>(nameof(CustomVariableGUI));
            show = !show;
            if (show) instance.UpdateRecords();
        }

        private void Awake ()
        {
            variableManager = Engine.GetService<ICustomVariableManager>();
            stateManager = Engine.GetService<IStateManager>();
        }

        private void OnEnable ()
        {
            variableManager.OnVariableUpdated += HandleVariableUpdated;
            stateManager.OnGameLoadFinished += HandleGameLoadFinished;
            stateManager.OnResetFinished += UpdateRecords;
            stateManager.OnRollbackFinished += UpdateRecords;
        }

        private void OnDisable ()
        {
            if (variableManager != null)
                variableManager.OnVariableUpdated -= HandleVariableUpdated;
            if (variableManager != null)
            {
                stateManager.OnGameLoadFinished -= HandleGameLoadFinished;
                stateManager.OnResetFinished -= UpdateRecords;
                stateManager.OnRollbackFinished -= UpdateRecords;
            }
        }

        private void OnGUI ()
        {
            if (!show) return;

            windowRect = GUI.Window(windowId, windowRect, DrawWindow, "Custom Variables");
        }

        private void DrawWindow (int windowId)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search: ", GUILayout.Width(50));
            search = GUILayout.TextField(search);
            GUILayout.EndHorizontal();

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            foreach (var record in records)
            {
                if (!string.IsNullOrEmpty(search) && !record.Key.StartsWith(search, StringComparison.OrdinalIgnoreCase)) continue;

                GUILayout.BeginHorizontal();
                GUILayout.TextField(record.Key, GUILayout.Width(width / 2f - 15));
                record.Value.EditedValue = GUILayout.TextField(record.Value.EditedValue);
                if (record.Value.Changed && GUILayout.Button("SET", GUILayout.Width(50)))
                    variableManager.SetVariableValue(record.Value.Name, record.Value.EditedValue);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Close Window")) show = false;

            GUI.DragWindow();
        }

        private void HandleVariableUpdated (CustomVariableUpdatedArgs args)
        {
            if (!show) return;

            // Checking if the update changed an existing variable.
            if (records.ContainsKey(args.Name))
            {
                records[args.Name].Value = args.Value;
                records[args.Name].EditedValue = args.Value;
                return;
            }

            // Adding a new variable.
            records.Add(args.Name, new Record (args.Name, args.Value));
        }

        private void HandleGameLoadFinished (GameSaveLoadArgs obj) => UpdateRecords();

        private void UpdateRecords ()
        {
            if (!show) return;

            records.Clear();
            foreach (var variable in variableManager.GetAllVariables())
                records.Add(variable.Name, new Record(variable.Name, variable.Value));
        }
    }
}
