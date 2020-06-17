// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine.UIElements;

namespace Naninovel
{
    public class ErrorLineView : ScriptLineView
    {
        public string CommandId { get; private set; }

        private readonly LineTextField valueField = default;

        public ErrorLineView (int lineIndex, string lineText, VisualElement container, string commandId, string error = default)
            : base(lineIndex, container)
        {
            CommandId = commandId;
            valueField = new LineTextField(value: lineText);
            Content.Add(valueField);
            if (!string.IsNullOrEmpty(error))
                tooltip = "Error: " + error;
        }

        public override string GenerateLineText () => valueField.value;
    }
}
