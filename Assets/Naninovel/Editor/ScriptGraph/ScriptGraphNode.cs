// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Naninovel
{
    /// <summary>
    /// Represents a node of <see cref="ScriptGraphView"/>.
    /// </summary>
    public class ScriptGraphNode : Node
    {
        public struct OutputPortData { public string ScriptName, Label; public Port Port; }

        public readonly Script Script;
        public readonly Dictionary<string, Port> InputPorts = new Dictionary<string, Port>();
        public readonly List<OutputPortData> OutputPorts = new List<OutputPortData>();

        private const string startLabel = "Start";

        private readonly ScriptsConfiguration config;
        private readonly List<Script> availableScripts;

        public ScriptGraphNode (ScriptsConfiguration config, Script script, List<Script> availableScripts)
        {
            this.config = config;
            this.availableScripts = availableScripts;
            title = script.Name;
            expanded = true;
            m_CollapseButton.Clear();
            m_CollapseButton.SetEnabled(false);
            capabilities = Capabilities.Ascendable | Capabilities.Selectable | Capabilities.Movable;
            Script = script;

            RegisterCallback<MouseDownEvent>(OnNodeMouseDown);

            InputPorts[string.Empty] = AddPort(Direction.Input, startLabel);

            foreach (var line in script.Lines)
            {
                if (line is LabelScriptLine labelLine)
                {
                    InputPorts[labelLine.LabelText] = AddPort(Direction.Input, $"{LabelScriptLine.IdentifierLiteral}{labelLine.LabelText}");
                    continue;
                }

                if (line is CommandScriptLine commandLine)
                {
                    if (commandLine.Command is Goto gotoCommand)
                        AddOutPort(gotoCommand, gotoCommand.Path.Name ?? line.ScriptName, gotoCommand.Path.NamedValue,
                            $"goto {gotoCommand.Path}".Replace(".null", string.Empty).Replace("null", string.Empty));
                    if (commandLine.Command is Gosub gosubCommand)
                        AddOutPort(gosubCommand, gosubCommand.Path.Name ?? line.ScriptName, gosubCommand.Path.NamedValue,
                            $"gosub {gosubCommand.Path}".Replace(".null", string.Empty).Replace("null", string.Empty));
                    continue;
                }
            }

            void AddOutPort (Command command, string gotoScript, string gotoLabel, string portLabel)
            {
                portLabel = $"{CommandScriptLine.IdentifierLiteral}{portLabel}";
                var portData = new OutputPortData {
                    ScriptName = gotoScript,
                    Label = string.IsNullOrEmpty(gotoLabel) ? string.Empty : gotoLabel,
                    Port = AddPort(Direction.Output, portLabel, command.ConditionalExpression)
                };
                OutputPorts.Add(portData);
            }
        }

        public override void BuildContextualMenu (ContextualMenuPopulateEvent evt) { }

        public HashSet<ScriptGraphNode> GetConnectedNodes ()
        {
            var inputNodes = InputPorts.SelectMany(p => p.Value.connections.Select(c => c.output.node as ScriptGraphNode)).Where(n => n != this);
            var outputNodes = OutputPorts.SelectMany(p => p.Port.connections.Select(c => c.input.node as ScriptGraphNode)).Where(n => n != this);
            var result = new HashSet<ScriptGraphNode>();
            result.UnionWith(inputNodes);
            result.UnionWith(outputNodes);
            return result;
        }

        private Port AddPort (Direction direction, string label, string tooltip = null)
        {
            var orientation = config.GraphOrientation == ScriptsConfiguration.GraphOrientationType.Vertical ? Orientation.Vertical : Orientation.Horizontal;
            var port = InstantiatePort(orientation, direction, direction == Direction.Input ? Port.Capacity.Multi : Port.Capacity.Single, typeof(Script));
            port.capabilities = Capabilities.Selectable;
            port.portName = label;
            port.Q<Label>("type").pickingMode = PickingMode.Position;
            if (!string.IsNullOrEmpty(tooltip))
            {
                port.tooltip = tooltip;
                port.Q<Label>("type").AddToClassList("if");
            }
            port.edgeConnector.activators.Clear(); // Prevents user from creating connections.
            port.RegisterCallback<MouseDownEvent>(OnPortMouseDown);
            inputContainer.Add(port);
            return port;
        }

        private void OnNodeMouseDown (MouseDownEvent evt)
        {
            if (evt.button == 0 && evt.clickCount >= 2 && ObjectUtils.IsValid(Script))
            {
                Selection.activeObject = Script;
                EditorGUIUtility.PingObject(Script);
            }
        }

        private void OnPortMouseDown (MouseDownEvent evt)
        {
            if (evt.button != 0) return;
            evt.StopImmediatePropagation();

            var port = evt.currentTarget as Port;
            if (port is null) return;

            (parent.parent.parent.parent as ScriptGraphView).edges.ForEach(e => { e.selected = false; e.UpdateEdgeControl(); });
            foreach (var edge in port.connections)
            {
                edge.selected = true;
                edge.UpdateEdgeControl();
            }

            var node = port.node as ScriptGraphNode;
            if (node is null) return;

            var gotoScript = default(Script);
            var gotoLabel = default(string);
            if (port.direction == Direction.Input)
            {
                gotoScript = node.Script;
                gotoLabel = node.InputPorts.FirstOrDefault(kv => kv.Value == port).Key;
            }
            else
            {
                var scriptName = OutputPorts.FirstOrDefault(d => d.Port == port).ScriptName;
                var script = availableScripts.FirstOrDefault(s => s.Name == scriptName);
                gotoScript = script;
                gotoLabel = OutputPorts.FirstOrDefault(d => d.Port == port).Label;
            }

            if (ObjectUtils.IsValid(gotoScript))
            {
                Selection.activeObject = gotoScript;
                EditorGUIUtility.PingObject(gotoScript);

                // Scroll to line.
                if (gotoScript.Lines.Count == 0) return;
                EditorApplication.delayCall += ScrollToLineDelayed;
                void ScrollToLineDelayed ()
                {
                    var editors = Resources.FindObjectsOfTypeAll<ScriptImporterEditor>();
                    if (editors.Length == 0) return;
                    var editorView = editors[0].VisualEditor;
                    var lineView = string.IsNullOrEmpty(gotoLabel) ? editorView.Lines.FirstOrDefault(l => l != null) :
                        editorView.Lines.FirstOrDefault(l => l?.LineIndex == gotoScript.GetLineIndexForLabel(gotoLabel));
                    if (lineView is null) EditorApplication.delayCall += ScrollToLineDelayed;
                    else editorView.ScrollToLine(lineView);
                }
            }
        }
    }
}
