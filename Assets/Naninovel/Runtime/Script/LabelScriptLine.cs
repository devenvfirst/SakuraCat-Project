// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="Script"/> line representing a text marker used to navigate within the script.
    /// </summary>
    [System.Serializable]
    public class LabelScriptLine : ScriptLine
    {
        /// <summary>
        /// Literal used to identify this type of lines.
        /// </summary>
        public const string IdentifierLiteral = "#";

        /// <summary>
        /// Text contents of the label (trimmed string after <see cref="IdentifierLiteral"/>).
        /// </summary>
        public string LabelText => labelText;

        [SerializeField] private string labelText = default;

        public LabelScriptLine (string scriptName, int lineIndex, string lineText)
            : base(scriptName, lineIndex, lineText) { }

        protected override void ParseLineText (string lineText, out string errors)
        {
            labelText = lineText.GetAfter(IdentifierLiteral)?.TrimFull();
            errors = null;
        }
    }
}
