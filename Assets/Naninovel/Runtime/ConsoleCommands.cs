// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides implementations of the built-in debug console commands.
    /// </summary>
    public static class ConsoleCommands
    {
        [ConsoleCommand("nav")]
        public static void ToggleScriptNavigator ()
        {
            var scriptManager = Engine.GetService<IScriptManager>();
            if (scriptManager.ScriptNavigator)
                scriptManager.ScriptNavigator.ToggleVisibility();
        }

        [ConsoleCommand("debug")]
        public static void ToggleDebugInfoGUI () => UI.DebugInfoGUI.Toggle();

        [ConsoleCommand("var")]
        public static void ToggleCustomVariableGUI () => UI.CustomVariableGUI.Toggle();

        #if UNITY_GOOGLE_DRIVE_AVAILABLE
        [ConsoleCommand("purge")]
        public static void PurgeCache ()
        {
            var manager = Engine.GetService<IResourceProviderManager>();
            if (manager is null) { Debug.LogError("Failed to retrieve provider manager."); return; }
            var googleDriveProvider = manager.GetProvider(ResourceProviderConfiguration.GoogleDriveTypeName) as GoogleDriveResourceProvider;
            if (googleDriveProvider is null) { Debug.LogError("Failed to retrieve google drive provider."); return; }
            googleDriveProvider.PurgeCache();
        }
        #endif

        [ConsoleCommand]
        public static void Play () => Engine.GetService<IScriptPlayer>()?.Play();

        [ConsoleCommand]
        public static void PlayScript (string name) => Engine.GetService<IScriptPlayer>()?.PreloadAndPlayAsync(name);

        [ConsoleCommand]
        public static void Stop () => Engine.GetService<IScriptPlayer>()?.Stop();

        [ConsoleCommand]
        public static async void Rewind (int line)
        {
            line = Mathf.Clamp(line, 1, int.MaxValue);
            var player = Engine.GetService<IScriptPlayer>();
            var playedScriptName = ObjectUtils.IsValid(player.PlayedScript) ? player.PlayedScript.Name : "null";
            var ok = await player.RewindAsync(line - 1);
            if (!ok) Debug.LogWarning($"Failed to rewind to line #{line} of script `{playedScriptName}`. Make sure the line exists in the script and it's playable (either a command or a generic text line). When rewinding forward, `@stop` commands can prevent reaching the target line. When rewinding backward the target line should've been previously played and be kept in the rollback stack (capacity controlled by `{nameof(StateConfiguration.StateRollbackSteps)}` property in state configuration).");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SetupDevelopmentConsole ()
        {
            if (Engine.Initialized) OnInitializationFinished();
            else Engine.OnInitializationFinished += OnInitializationFinished;

            void OnInitializationFinished ()
            {
                Engine.OnInitializationFinished -= OnInitializationFinished;
                if (!Engine.Configuration.EnableDevelopmentConsole) return;

                ConsoleGUI.ToggleKey = Engine.Configuration.ToggleConsoleKey;
                ConsoleGUI.Initialize();

                // Process input starting with `@` as naninovel commands.
                InputPreprocessor.AddPreprocessor(ProcessActionInput);
            }
        }

        private static string ProcessActionInput (string input)
        {
            if (input is null || !input.StartsWithFast(CommandScriptLine.IdentifierLiteral)) return input;

            var commandText = input.GetAfterFirst(CommandScriptLine.IdentifierLiteral);
            var command = Commands.Command.FromScriptText("Console", 0, 0, commandText, out var errors);
            if (!string.IsNullOrEmpty(errors))
                Debug.LogWarning($"Failed to parse `{input}` command from development console.");
            if (command is null) return null;

            if (command.ShouldExecute)
                command.ExecuteAsync();
            return null;
        }
    }
}
