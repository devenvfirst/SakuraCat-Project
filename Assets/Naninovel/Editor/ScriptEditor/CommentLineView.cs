// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class CommentLineView : ScriptLineView
    {
        private readonly LineTextField valueField;

        public CommentLineView (int lineIndex, string lineText, VisualElement container)
            : base(lineIndex, container)
        {
            var value = lineText.GetAfterFirst(CommentScriptLine.IdentifierLiteral)?.TrimFull();
            valueField = new LineTextField(CommentScriptLine.IdentifierLiteral, value);
            valueField.multiline = true;
            Content.Add(valueField);
        }

        public override string GenerateLineText () => $"{CommentScriptLine.IdentifierLiteral} {valueField.value}";
    }
}
