// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="Script"/> line representing a commentary left by the author of the script.
    /// </summary>
    [System.Serializable]
    public class CommentScriptLine : ScriptLine
    {
        /// <summary>
        /// Literal used to identify this type of lines.
        /// </summary>
        public const string IdentifierLiteral = ";";

        /// <summary>
        /// Text contents of the commentary (trimmed string after the <see cref="IdentifierLiteral"/>).
        /// </summary>
        public string CommentText => commentText;

        [SerializeField] private string commentText = default;

        public CommentScriptLine (string scriptName, int lineIndex, string lineText)
            : base(scriptName, lineIndex, lineText) { }

        protected override void ParseLineText (string lineText, out string errors)
        {
            commentText = lineText.GetAfterFirst(IdentifierLiteral)?.TrimFull();
            errors = null;
        }
    }
}
