// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="Script"/> line representing text to print.
    /// Could contain author (actor) ID and (optionally) appearance delimeted by <see cref="AuthorAppearanceLiteral"/> 
    /// at the start of the line followed by <see cref="AuthorIdLiteral"/>. Can also have multiple injected (inlined)
    /// <see cref="Command"/> enclosed in `[` and `]`/>.
    /// </summary>
    [System.Serializable]
    public class GenericTextScriptLine : ScriptLine
    {
        /// <summary>
        /// Literal used to separate author actor ID and (optionally) appearance before the text to print.
        /// </summary>
        public const string AuthorIdLiteral = ": ";
        /// <summary>
        /// Literal used to separate author actor ID from appearance.
        /// </summary>
        public const string AuthorAppearanceLiteral = ".";
        /// <summary>
        /// A list of <see cref="Command"/> inlined in this line.
        /// </summary>
        public ReadOnlyCollection<Command> InlinedCommands => inlinedCommands.AsReadOnly();

        [SerializeReference] private List<Command> inlinedCommands = default;

        public GenericTextScriptLine (string scriptName, int lineIndex, string lineText)
            : base(scriptName, lineIndex, lineText) { }

        protected override void ParseLineText (string lineText, out string errors)
        {
            inlinedCommands = new List<Command>();
            errors = null;

            // Extract author ID (if present).
            var authorId = lineText.GetBefore(AuthorIdLiteral);
            if (!string.IsNullOrEmpty(authorId) && !authorId.Any(char.IsWhiteSpace) && !authorId.StartsWithFast("\""))
                lineText = lineText.GetAfterFirst(AuthorIdLiteral);
            else authorId = null;

            // Extract and apply author appearance (if present).
            if (!string.IsNullOrEmpty(authorId) && authorId.Contains(AuthorAppearanceLiteral))
            {
                var authorAppearance = authorId.GetAfter(AuthorAppearanceLiteral);
                authorId = authorId.GetBefore(AuthorAppearanceLiteral);

                var appearanceChangeCommand = new ModifyCharacter { PlaybackSpot = new PlaybackSpot(ScriptName, LineIndex, 0) };
                appearanceChangeCommand.IdAndAppearance = new NamedString(authorId, authorAppearance);
                appearanceChangeCommand.Wait = false;
                inlinedCommands.Add(appearanceChangeCommand);
            }

            // Collect all inlined command strings (text inside not-escaped square brackets).
            var inlinedCommandMatches = Regex.Matches(lineText, @"(?<!\\)\[.+?(?<!\\)\]").Cast<Match>().ToList();

            // In case no inlined commands found, just add a single @print command line.
            if (inlinedCommandMatches.Count == 0)
            {
                var print = CreatePrintTextCommand(lineText, out errors, authorId);
                if (!string.IsNullOrEmpty(errors)) return;
                inlinedCommands.Add(print);
                return;
            }

            var printedTextBefore = false;
            for (int i = 0; i < inlinedCommandMatches.Count; i++)
            {
                // Check if we need to print any text before the current inlined command.
                var precedingGenericText = StringUtils.TrySubset(lineText,
                    i > 0 ? inlinedCommandMatches[i - 1].GetEndIndex() + 1 : 0,
                    inlinedCommandMatches[i].Index - 1);
                if (!string.IsNullOrEmpty(precedingGenericText))
                {
                    var prePrint = CreatePrintTextCommand(precedingGenericText, out errors, authorId, printedTextBefore ? (bool?)false : null, false, printedTextBefore ? (int?)0 : null);
                    if (!string.IsNullOrEmpty(errors)) return;
                    inlinedCommands.Add(prePrint);
                    printedTextBefore = true;
                }

                // Extract inlined command body text.
                var commandText = inlinedCommandMatches[i].ToString().GetBetween("[", "]").TrimFull();
                var command = Command.FromScriptText(ScriptName, LineIndex, inlinedCommands.Count, commandText, out errors);
                if (!string.IsNullOrEmpty(errors)) return;
                if (command is WaitForInput wait && inlinedCommands.LastOrDefault() is PrintText print)
                    print.WaitForInput = true;
                else inlinedCommands.Add(command);
            }

            // Check if we need to print any text after the last inlined command.
            var lastGenericText = StringUtils.TrySubset(lineText,
                     inlinedCommandMatches.Last().GetEndIndex() + 1,
                     lineText.Length - 1);
            if (!string.IsNullOrEmpty(lastGenericText))
            {
                var postPrint = CreatePrintTextCommand(lastGenericText, out errors, authorId, printedTextBefore ? (bool?)false : null, false, printedTextBefore ? (int?)0 : null);
                if (!string.IsNullOrEmpty(errors)) return;
                inlinedCommands.Add(postPrint);
            }

            // Add wait input command at the end; except when generic text contains a [skipInput] flag.
            if (lineText?.IndexOf(nameof(SkipInput), StringComparison.OrdinalIgnoreCase) == -1 &&
                !(inlinedCommands.LastOrDefault() is WaitForInput)) // ...or wait is already requested, eg `[i]` is the only content of the line.
            {
                if (inlinedCommands.LastOrDefault() is PrintText print)
                    print.WaitForInput = true;
                else
                {
                    var waitCommand = new WaitForInput { PlaybackSpot = new PlaybackSpot(ScriptName, LineIndex, inlinedCommands.Count) };
                    inlinedCommands.Add(waitCommand);
                }
            }
        }

        private PrintText CreatePrintTextCommand (string printTextValue, out string errors, string authorIdTextValue = null, bool? resetPrinter = null, bool? waitForInput = null, int? br = null)
        {
            // Un-escape square brackets ment to be printed.
            printTextValue = printTextValue.Replace("\\[", "[").Replace("\\]", "]");

            var printCommand = new PrintText {
                PlaybackSpot = new PlaybackSpot(ScriptName, LineIndex, inlinedCommands.Count),
                Text = new StringParameter(),
                AuthorId = new StringParameter()
            };
            printCommand.Text.SetValueFromScriptText(printCommand.PlaybackSpot, printTextValue, out var paramErrors);
            if (paramErrors != null) { errors = paramErrors; return null; }
            if (authorIdTextValue != null)
            {
                printCommand.AuthorId.SetValueFromScriptText(printCommand.PlaybackSpot, authorIdTextValue, out paramErrors);
                if (paramErrors != null) { errors = paramErrors; return null; }
            }

            if (resetPrinter.HasValue)
                printCommand.ResetPrinter = resetPrinter.Value;
            if (waitForInput.HasValue)
                printCommand.WaitForInput = waitForInput.Value;
            if (br.HasValue)
                printCommand.LineBreaks = br.Value;

            errors = null;
            return printCommand;
        }
    }
}
