// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;

namespace Naninovel
{
    public class CommandLineView : ScriptLineView
    {
        private struct ParameterFieldData { public string Id, Name, Value; public bool Nameless; }

        public string CommandId { get; private set; }

        private readonly List<LineTextField> parameterFields = new List<LineTextField>();
        private readonly List<ParameterFieldData> delayedAddFields = new List<ParameterFieldData>();

        private bool hideParameters = default;

        private CommandLineView (int lineIndex, VisualElement container)
            : base(lineIndex, container) { }

        public static ScriptLineView CreateDefault (int lineIndex, string commandId, VisualElement container, bool hideParameters)
        {
            return CreateOrError(lineIndex, $"{CommandScriptLine.IdentifierLiteral}{commandId}", container, hideParameters);
        }

        public static ScriptLineView CreateOrError (int lineIndex, string lineText, VisualElement container, bool hideParameters)
        {
            var commandBodyText = lineText.GetAfterFirst(CommandScriptLine.IdentifierLiteral);
            var commandId = commandBodyText.GetBefore(" ") ?? commandBodyText.GetBefore("\t") ?? commandBodyText.Trim();
            var commandType = Command.ResolveCommandType(commandId);
            if (commandType is null) return Error("Failed to resolve command type.");

            var commandLineView = new CommandLineView(lineIndex, container);

            var nameLabel = new Label(commandId);
            nameLabel.name = "InputLabel";
            nameLabel.AddToClassList("Inlined");
            commandLineView.Content.Add(nameLabel);
            commandLineView.CommandId = commandId;
            commandLineView.hideParameters = hideParameters;

            var paramFields = commandType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => typeof(ICommandParameter).IsAssignableFrom(f.FieldType));
            var paramValues = typeof(Command).GetMethod("ExtractParameters", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new[] { commandBodyText, string.Empty }) as LiteralMap<string>;
            foreach (var paramField in paramFields)
            {
                var paramAlias = paramField.GetCustomAttribute<Command.ParameterAliasAttribute>()?.Alias;
                var paramId = paramAlias ?? char.ToLowerInvariant(paramField.Name[0]) + paramField.Name.Substring(1);
                var nameless = string.IsNullOrEmpty(paramId);

                paramValues.TryGetValue(paramId, out var valueText);
                if (valueText is null) 
                    paramValues.TryGetValue(paramField.Name, out valueText);

                var data = new ParameterFieldData { Id = paramId, Name = paramField.Name, Value = valueText, Nameless = nameless };
                if (!data.Nameless && hideParameters && string.IsNullOrEmpty(data.Value))
                    commandLineView.delayedAddFields.Add(data); // Add un-assigned fields on hover for better init performance.
                else commandLineView.AddParameterField(data);
            }

            return commandLineView;

            ErrorLineView Error (string error) => new ErrorLineView(lineIndex, lineText, container, commandId, error);
        }

        public override string GenerateLineText ()
        {
            var result = $"{CommandScriptLine.IdentifierLiteral}{CommandId}";
            var namelessParamField = parameterFields.FirstOrDefault(f => string.IsNullOrEmpty(f.label));
            if (namelessParamField != null)
                result += $" {HandleQuotes(namelessParamField.value)}";

            foreach (var field in parameterFields)
                if (!string.IsNullOrEmpty(field.label) && !string.IsNullOrWhiteSpace(field.value))
                    result += $" {field.label}:{HandleQuotes(field.value)}";
            return result;
        }

        protected override void ApplyFocusedStyle ()
        {
            base.ApplyFocusedStyle();

            if (DragManipulator.Active) return;
            ShowUnAssignedNamedFields();
        }

        protected override void ApplyNotFocusedStyle ()
        {
            base.ApplyNotFocusedStyle();

            HideUnAssignedNamedFields();
        }

        protected override void ApplyHoveredStyle ()
        {
            base.ApplyHoveredStyle();

            if (DragManipulator.Active) return;
            ShowUnAssignedNamedFields();
        }

        protected override void ApplyNotHoveredStyle ()
        {
            base.ApplyNotHoveredStyle();

            if (FocusedLine == this) return;
            HideUnAssignedNamedFields();
        }

        private void AddParameterField (ParameterFieldData data)
        {
            var textField = new LineTextField(data.Id, data.Value ?? string.Empty);
            if (data.Nameless) textField.tooltip = data.Name;
            else textField.AddToClassList("NamedParameterLabel");
            parameterFields.Add(textField);
            // Show the un-assigned named parameters only when hovered or focused.
            if (data.Nameless || !hideParameters || !string.IsNullOrEmpty(data.Value))
                Content.Add(textField);
        }

        private void ShowUnAssignedNamedFields ()
        {
            if (!hideParameters) return;

            // Add un-assigned fields in case they weren't added on init.
            if (delayedAddFields.Count > 0)
            {
                foreach (var data in delayedAddFields)
                    AddParameterField(data);
                delayedAddFields.Clear();
            }

            foreach (var field in parameterFields)
                if (!Content.Contains(field))
                    Content.Add(field);
        }

        private void HideUnAssignedNamedFields ()
        {
            if (!hideParameters) return;

            foreach (var field in parameterFields)
                if (!string.IsNullOrEmpty(field.label) && string.IsNullOrWhiteSpace(field.value) && Content.Contains(field))
                    Content.Remove(field);
        }

        private string HandleQuotes (string value)
        {
            // We're doing the reverse in command script line parsing. 
            if (value.Contains(" ")) return $"\"{value.Replace("\"", "\\\"")}\"";
            return value;
        }
    }
}
