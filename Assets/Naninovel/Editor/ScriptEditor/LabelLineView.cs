// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class LabelLineView : ScriptLineView
    {
        public readonly LineTextField ValueField;

        public LabelLineView (int lineIndex, string lineText, VisualElement container)
            : base(lineIndex, container)
        {
            var value = lineText.GetAfterFirst(LabelScriptLine.IdentifierLiteral)?.TrimFull();
            ValueField = new LineTextField(LabelScriptLine.IdentifierLiteral, value);
            Content.Add(ValueField);
        }

        public override string GenerateLineText () => $"{LabelScriptLine.IdentifierLiteral} {ValueField.value}";
    }
}
