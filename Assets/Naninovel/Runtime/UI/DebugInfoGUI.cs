// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.UI
{
    public class DebugInfoGUI : MonoBehaviour
    {
        private const int windowId = 0;

        private static DebugInfoGUI instance;
        private Rect windowRect = new Rect(20, 20, 250, 100);
        private bool show;
        private EngineVersion version;
        private IScriptPlayer player;
        private IAudioManager audioManager;
        private IStateManager stateManager;
        private string lastActionInfo, lastAutoVoiceName;

        public static void Toggle ()
        {
            if (instance == null)
                instance = Engine.CreateObject<DebugInfoGUI>(nameof(DebugInfoGUI));

            instance.show = !instance.show;

            if (instance.show && instance.player != null)
                instance.HandleActionExecuted(instance.player.PlayedCommand);
        }

        private void Awake ()
        {
            version = EngineVersion.LoadFromResources();
            player = Engine.GetService<IScriptPlayer>();
            audioManager = Engine.GetService<IAudioManager>();
            stateManager = Engine.GetService<IStateManager>();
        }

        private void OnEnable ()
        {
            player.OnCommandExecutionStart += HandleActionExecuted;
            stateManager.OnRollbackFinished += HandleRollbackFinished;
        }

        private void OnDisable ()
        {
            player.OnCommandExecutionStart -= HandleActionExecuted;
            stateManager.OnRollbackFinished -= HandleRollbackFinished;
        }

        private void OnGUI ()
        {
            if (!show) return;

            windowRect = GUI.Window(windowId, windowRect, DrawWindow, 
                string.IsNullOrEmpty(lastActionInfo) ? $"Naninovel ver. {version.Version}" : lastActionInfo);
        }

        private void DrawWindow (int windowID)
        {
            if (player.PlayedCommand != null)
            {
                if (!string.IsNullOrEmpty(lastAutoVoiceName))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Auto Voice: ");
                    GUILayout.TextField(lastAutoVoiceName);
                    GUILayout.EndHorizontal();
                }

                GUILayout.FlexibleSpace();
                GUI.enabled = !player.Playing;
                if (!player.Playing && GUILayout.Button("Play")) player.Play();
                GUI.enabled = player.Playing;
                if (player.Playing && GUILayout.Button("Stop")) player.Stop();
                GUI.enabled = true;
                if (GUILayout.Button("Close Window")) show = false;
            }

            GUI.DragWindow();
        }

        private void HandleActionExecuted (Commands.Command command)
        {
            if (command is null) return;

            lastActionInfo = player?.PlayedCommand?.PlaybackSpot.ToString();

            if (audioManager != null && audioManager.Configuration.EnableAutoVoicing && command is Commands.PrintText printAction)
                lastAutoVoiceName = $"{player.PlayedScript.Name}/{printAction.PlaybackSpot.LineNumber}.{printAction.PlaybackSpot.InlineIndex}";
        }

        private void HandleRollbackFinished () => HandleActionExecuted(player.PlayedCommand);
    }
}
