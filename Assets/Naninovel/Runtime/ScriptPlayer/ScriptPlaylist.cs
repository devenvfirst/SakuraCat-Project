// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a list of <see cref="Command"/> based on the contents of a <see cref="Script"/>.
    /// </summary>
    public class ScriptPlaylist : List<Command>
    {
        /// <summary>
        /// Name of the script from which the contained commands were extracted.
        /// </summary>
        public readonly string ScriptName;

        /// <summary>
        /// Creates new instance from the provided commands collection.
        /// </summary>
        public ScriptPlaylist (string scriptName, IEnumerable<Command> commands)
            : base(commands)
        {
            ScriptName = scriptName;
        }

        /// <summary>
        /// Creates new instance from the provided script.
        /// When <paramref name="scriptManager"/> provided, will attempt to use a corresponding localization script.
        /// </summary>
        public ScriptPlaylist (Script script, IScriptManager scriptManager = null)
        {
            ScriptName = script.name;
            var localizationScript = scriptManager?.GetLocalizationScriptFor(script);
            var commands = script.ExtractCommands(localizationScript);
            AddRange(commands);
        }

        /// <summary>
        /// Preloads and holds all the resources required to execute <see cref="Command.IPreloadable"/> commands contained in this list.
        /// </summary>
        public async UniTask HoldResourcesAsync () => await HoldResourcesAsync(0, Count - 1);

        /// <summary>
        /// Preloads and holds resources required to execute <see cref="Command.IPreloadable"/> commands in the specified range.
        /// </summary>
        public async UniTask HoldResourcesAsync (int startCommandIndex, int endCommandIndex)
        {
            if (Count == 0) return;

            if (!this.IsIndexValid(startCommandIndex) || !this.IsIndexValid(endCommandIndex) || endCommandIndex < startCommandIndex)
            {
                Debug.LogError($"Failed to preload `{ScriptName}` script resources: [{startCommandIndex}, {endCommandIndex}] is not a valid range.");
                return;
            }

            var commandsToHold = GetRange(startCommandIndex, (endCommandIndex + 1) - startCommandIndex).OfType<Command.IPreloadable>();
            await UniTask.WhenAll(commandsToHold.Select(cmd => cmd.HoldResourcesAsync()));
        }

        /// <summary>
        /// Releases all the held resources required to execute <see cref="Command.IPreloadable"/> commands contained in this list.
        /// </summary>
        public void ReleaseResources () => ReleaseResources(0, Count - 1);

        /// <summary>
        /// Releases all the held resources required to execute <see cref="Command.IPreloadable"/> commands in the specified range.
        /// </summary>
        public void ReleaseResources (int startCommandIndex, int endCommandIndex)
        {
            if (Count == 0) return;

            if (!this.IsIndexValid(startCommandIndex) || !this.IsIndexValid(endCommandIndex) || endCommandIndex < startCommandIndex)
            {
                Debug.LogError($"Failed to unload `{ScriptName}` script resources: [{startCommandIndex}, {endCommandIndex}] is not a valid range.");
                return;
            }

            var commandsToRelease = GetRange(startCommandIndex, (endCommandIndex + 1) - startCommandIndex).OfType<Command.IPreloadable>();
            foreach (var cmd in commandsToRelease)
                cmd.ReleaseResources();
        }

        /// <summary>
        /// Returns a <see cref="Command"/> at the provided index; null if not found.
        /// </summary>
        public Command GetCommandByIndex (int commandIndex) => this.IsIndexValid(commandIndex) ? this[commandIndex] : null;

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandScriptLine"/> with provided line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandByLine (int lineIndex, int inlineIndex) => Find(a => a.PlaybackSpot.LineIndex == lineIndex && a.PlaybackSpot.InlineIndex == inlineIndex);

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandScriptLine"/> located at or after the provided line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandAfterLine (int lineIndex, int inlineIndex) => this.FirstOrDefault(a => a.PlaybackSpot.LineIndex >= lineIndex && a.PlaybackSpot.InlineIndex >= inlineIndex);

        /// <summary>
        /// Finds a <see cref="Command"/> that was created from a <see cref="CommandScriptLine"/> located at or before the provided line and inline indexes; null if not found.
        /// </summary>
        public Command GetCommandBeforeLine (int lineIndex, int inlineIndex) => this.LastOrDefault(a => a.PlaybackSpot.LineIndex <= lineIndex && a.PlaybackSpot.InlineIndex <= inlineIndex);

        /// <summary>
        /// Finds index of a contained command with the provided playback spot or -1 when not found.
        /// </summary>
        public int IndexOf (PlaybackSpot playbackSpot)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].PlaybackSpot == playbackSpot) return i;
            return -1;
        }
    }
}
