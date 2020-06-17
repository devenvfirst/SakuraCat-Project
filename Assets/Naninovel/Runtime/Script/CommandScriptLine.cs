// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="Script"/> line representing a <see cref="Commands.Command"/>.
    /// </summary>
    [System.Serializable]
    public class CommandScriptLine : ScriptLine
    {
        /// <summary>
        /// Literal used to identify this type of lines.
        /// </summary>
        public const string IdentifierLiteral = "@";
        /// <summary>
        /// The command which this line represents.
        /// </summary>
        public Command Command => command;

        [SerializeReference] private Command command = default;

        public CommandScriptLine (string scriptName, int lineIndex, string lineText)
            : base(scriptName, lineIndex, lineText) { }

        protected override void ParseLineText (string lineText, out string errors)
        {
            var commandText = lineText.GetAfterFirst(IdentifierLiteral);
            command = Command.FromScriptText(ScriptName, LineIndex, 0, commandText, out errors);
        }
    }
}
