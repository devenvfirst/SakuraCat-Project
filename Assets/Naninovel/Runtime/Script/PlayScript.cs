// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows to play a <see cref="Script"/> or execute script commands via Unity API.
    /// </summary>
    public class PlayScript : MonoBehaviour
    {
        [Tooltip("The script asset to play.")]
        [ResourcesPopup(ScriptsConfiguration.DefaultScriptsPathPrefix, ScriptsConfiguration.DefaultScriptsPathPrefix, "None (disabled)")]
        [SerializeField] private string scriptName = default;
        [TextArea(3, 10), Tooltip("The naninovel script text (commands) to execute; has no effect when `Script Name` is specified. Argument of the event (if any) can be referenced in the script text via `{arg}` expression. Conditional block commands (if, else, etc) are not supported.")]
        [SerializeField] private string scriptText = default;

        private string argument;

        public void Play ()
        {
            argument = null;
            PlayScriptAsync();
        }

        public void Play (string argument)
        {
            this.argument = argument;
            PlayScriptAsync();
        }

        public void Play (float argument)
        {
            this.argument = argument.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PlayScriptAsync();
        }

        public void Play (int argument)
        {
            this.argument = argument.ToString(System.Globalization.CultureInfo.InvariantCulture);
            PlayScriptAsync();
        }

        public void Play (bool argument)
        {
            this.argument = argument.ToString(System.Globalization.CultureInfo.InvariantCulture).ToLower();
            PlayScriptAsync();
        }

        private async void PlayScriptAsync ()
        {
            if (!string.IsNullOrEmpty(scriptName))
            {
                var player = Engine.GetService<IScriptPlayer>();
                if (player is null)
                {
                    Debug.LogError($"Failed to play a script via `{nameof(PlayScript)}` component attached to `{gameObject.name}` game object: script player service is not available.");
                    return;
                }
                await player.PreloadAndPlayAsync(scriptName);
                return;
            }

            if (!string.IsNullOrWhiteSpace(scriptText))
            {
                var text = string.IsNullOrEmpty(argument) ? scriptText : scriptText.Replace("{arg}", argument);
                var script = Script.FromScriptText($"`{name}` generated script", text);
                var playlist = new ScriptPlaylist(script);
                foreach (var command in playlist)
                    if (command.ShouldExecute)
                    {
                        if (!command.Wait && !(command is Commands.Command.IForceWait))
                            command.ExecuteAsync().Forget();
                        else await command.ExecuteAsync();
                    }
            }
        }
    }
}
