// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Representation of a text file used to author naninovel scenario flow.
    /// </summary>
    [System.Serializable]
    public class Script : ScriptableObject
    {
        /// <summary>
        /// Name of the script asset.
        /// </summary>
        public string Name => name;
        /// <summary>
        /// The lines this script asset contains.
        /// </summary>
        public ReadOnlyCollection<ScriptLine> Lines => lines.AsReadOnly();

        [SerializeReference] private List<ScriptLine> lines = default;

        /// <summary>
        /// Creates new instance by parsing the provided serialized script text.
        /// </summary>
        /// <param name="scriptName">Name of the script asset.</param>
        /// <param name="scriptText">The serialized script text to parse.</param>
        public static Script FromScriptText (string scriptName, string scriptText)
        {
            var asset = CreateInstance<Script>();
            asset.name = scriptName;
            asset.lines = new List<ScriptLine>();

            var scriptLinesText = SplitScriptText(scriptText);
            for (int i = 0; i < scriptLinesText.Length; i++)
            {
                var lineText = scriptLinesText[i].TrimFull();
                var lineType = ResolveLineType(lineText);
                var line = Activator.CreateInstance(lineType, asset.Name, i, lineText) as ScriptLine;
                asset.lines.Add(line);
            }

            return asset;
        }

        /// <summary>
        /// Evaluates type of the line that could be parsed of the provided script line text string,
        /// based on the identifier literal placed at the start of the string.
        /// </summary>
        public static Type ResolveLineType (string lineText)
        {
            if (string.IsNullOrWhiteSpace(lineText))
                return typeof(CommentScriptLine);
            else if (lineText.StartsWithFast(CommandScriptLine.IdentifierLiteral))
                return typeof(CommandScriptLine);
            else if (lineText.StartsWithFast(CommentScriptLine.IdentifierLiteral))
                return typeof(CommentScriptLine);
            else if (lineText.StartsWithFast(LabelScriptLine.IdentifierLiteral))
                return typeof(LabelScriptLine);
            else return typeof(GenericTextScriptLine);
        }

        /// <summary>
        /// Splits provided script text string into individual script lines.
        /// </summary>
        public static string[] SplitScriptText (string scriptText)
        {
            // uFEFF - BOM, u200B - zero-width space.
            return scriptText?.Trim(new char[] { '\uFEFF', '\u200B' })?.SplitByNewLine() ?? new[] { string.Empty };
        }

        /// <summary>
        /// Logs a message to the console with the additional script line position info.
        /// </summary>
        public static void LogWithPosition (string scriptName, int lineNumber, int inlineIndex, string message, LogType logType = LogType.Log)
        {
            var line = $"line #{lineNumber}{(inlineIndex <= 0 ? string.Empty : $".{inlineIndex}")}";
            var fullMessage = string.IsNullOrEmpty(scriptName) ? $"Transient Naninovel script: {message}" : $"Naninovel script `{scriptName}` at {line}: {message}";
            Debug.LogFormat(logType, default, default, fullMessage);
        }

        /// <summary>
        /// Logs a message to the console with the additional script line position info.
        /// </summary>
        public static void LogWithPosition (PlaybackSpot playbackSpot, string message, LogType logType = LogType.Log)
        {
            LogWithPosition(playbackSpot.ScriptName, playbackSpot.LineNumber, playbackSpot.InlineIndex, message, logType);
        }

        /// <summary>
        /// Collects all the contained commands (preserving the order).
        /// </summary>
        /// <param name="localizationScript">When provided, will attempt to replace localizable commands based on <see cref="ScriptLine.LineHash"/>.</param>
        /// <remarks>
        /// Localization script is expected in the following format:
        /// # Localized (source) line hash (as label line)
        /// ; Text to localize from the source script (as comment line, optional)
        /// The localized text to use as replacement (as generic or command lines)
        /// More localized text (mutliple localized lines are possible per one source line)
        /// </remarks>
        public List<Command> ExtractCommands (Script localizationScript = null)
        {
            var commands = new List<Command>();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line is CommentScriptLine || line is LabelScriptLine) continue;

                var li = localizationScript ? localizationScript.GetLineIndexForLabel(line.LineHash) : -1;
                if (li > -1) // Localized lines available.
                {
                    var replacedAnything = false;
                    var inlineIndex = 0;
                    while (localizationScript.lines.IsIndexValid(li + 1))
                    {
                        li++;
                        var localizedLine = localizationScript.lines[li];
                        if (localizedLine is CommentScriptLine) continue;
                        if (localizedLine is LabelScriptLine) break;
                        OverrideCommandIndexInLine(localizedLine, i, ref inlineIndex);
                        AddCommandsFromLine(localizedLine);
                        replacedAnything = true;
                    }

                    if (replacedAnything) continue;
                    // Else: no localized lines found; fallback to the source line.
                }

                AddCommandsFromLine(line);
            }

            return commands;

            void AddCommandsFromLine (ScriptLine line)
            {
                if (line is CommandScriptLine commandLine)
                    commands.Add(commandLine.Command);
                else if (line is GenericTextScriptLine genericLine)
                    commands.AddRange(genericLine.InlinedCommands);
            }

            void OverrideCommandIndexInLine (ScriptLine line, int lineIndex, ref int inlineIndex)
            {
                if (line is CommandScriptLine commandLine)
                {
                    commandLine.Command.PlaybackSpot = new PlaybackSpot(commandLine.Command.PlaybackSpot.ScriptName, lineIndex, inlineIndex);
                    inlineIndex++;
                }
                else if (line is GenericTextScriptLine genericLine)
                    for (int i = 0; i < genericLine.InlinedCommands.Count; i++)
                    {
                        var command = genericLine.InlinedCommands[i];
                        command.PlaybackSpot = new PlaybackSpot(command.PlaybackSpot.ScriptName, lineIndex, inlineIndex);
                        inlineIndex++;
                    }
            }
        }

        /// <summary>
        /// Checks whether a <see cref="LabelScriptLine"/> with the provided value exists in this script.
        /// </summary>
        public bool LabelExists (string label)
        {
            return lines.Exists(l => l is LabelScriptLine labelLine && labelLine.LabelText.EqualsFast(label));
        }

        /// <summary>
        /// Attempts to retrieve index of a <see cref="LabelScriptLine"/> with the provided <see cref="LabelScriptLine.LabelText"/>.
        /// Returns -1 in case the label is not found.
        /// </summary>
        public int GetLineIndexForLabel (string label)
        {
            foreach (var line in lines)
                if (line is LabelScriptLine labelLine && labelLine.LabelText.EqualsFast(label))
                    return labelLine.LineIndex;
            return -1;
        }

        /// <summary>
        /// Returns first <see cref="LabelScriptLine.LabelText"/> located above line with the provided index.
        /// Returns null when not found.
        /// </summary>
        public string GetLabelForLine (int lineIndex)
        {
            if (!lines.IsIndexValid(lineIndex)) return null;
            for (var i = lineIndex; i >= 0; i--)
                if (lines[i] is LabelScriptLine labelLine)
                    return labelLine.LabelText;
            return null;
        }

        /// <summary>
        /// Returns first <see cref="CommentScriptLine.CommentText"/> located above line with the provided index.
        /// Returns null when not found.
        /// </summary>
        public string GetCommentForLine (int lineIndex)
        {
            if (!lines.IsIndexValid(lineIndex)) return null;
            for (var i = lineIndex; i >= 0; i--)
                if (lines[i] is CommentScriptLine commentLine)
                    return commentLine.CommentText;
            return null;
        }
    }
}
