// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Represents a <see cref="Script"/> command.
    /// </summary>
    [System.Serializable]
    public abstract class Command : ISerializationCallbackReceiver
    {
        /// <summary>
        /// Implementing <see cref="Command"/> contains an asynchronous logic that should 
        /// always be awaited before executing next commands; <see cref="Wait"/> is ignored.
        /// </summary>
        public interface IForceWait { }

        /// <summary>
        /// Implementing <see cref="Command"/> will be included in localization scripts.
        /// </summary>
        public interface ILocalizable { }

        /// <summary>
        /// Implementing <see cref="Command"/> is able to preload, hold and release resources required for execution.
        /// </summary>
        public interface IPreloadable
        {
            /// <summary>
            /// Preloads resources required for the command execution.
            /// </summary>
            UniTask HoldResourcesAsync ();
            /// <summary>
            /// Unloads resources loaded with <see cref="HoldResourcesAsync"/>.
            /// </summary>
            void ReleaseResources ();
        }

        /// <summary>
        /// Assigns an alias name for <see cref="Command"/>.
        /// Aliases can be used instead of the command IDs (type names) to reference commands in naninovel script.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class CommandAliasAttribute : Attribute
        {
            public string Alias { get; }

            public CommandAliasAttribute (string alias)
            {
                Alias = alias;
            }
        }

        /// <summary>
        /// Registers the field as a required <see cref="ICommandParameter"/> logging error when it's not supplied in naninovel scripts.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
        public sealed class RequiredParameterAttribute : Attribute { }

        /// <summary>
        /// Assigns an alias name to a <see cref="ICommandParameter"/> field allowing it to be used instead of the field name in naninovel scripts.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
        public sealed class ParameterAliasAttribute : Attribute
        {
            /// <summary>
            /// Alias name of the parameter.
            /// </summary>
            public string Alias { get; }

            /// <param name="alias">Alias name of the parameter.</param>
            public ParameterAliasAttribute (string alias)
            {
                Alias = alias;
            }
        }

        /// <summary>
        /// Use this alias to specify a nameless command parameter.
        /// </summary>
        public const string NamelessParameterAlias = "";
        /// <summary>
        /// Literal used to assign paramer values to their IDs.
        /// </summary>
        public const string ParameterAssignLiteral = ":";
        /// <summary>
        /// Contains all the available <see cref="Command"/> types in the application domain, 
        /// indexed by command alias (if available) or implementing type name. Keys are case-insensitive.
        /// </summary>
        public static readonly LiteralMap<Type> CommandTypes = GetCommandTypes();

        /// <summary>
        /// In case the command belongs to a <see cref="Script"/> asset, represents position inside the script.
        /// </summary>
        public PlaybackSpot PlaybackSpot { get => playbackSpot; set => playbackSpot = value; }
        /// <summary>
        /// Whether this command should be executed, as per <see cref="ConditionalExpression"/>.
        /// </summary>
        public bool ShouldExecute => string.IsNullOrEmpty(ConditionalExpression) || ExpressionEvaluator.Evaluate<bool>(ConditionalExpression);

        /// <summary>
        /// Whether the script player should wait for the async command execution before playing next command.
        /// </summary>
        public BooleanParameter Wait = true;
        /// <summary>
        /// A boolean [script expression](/guide/script-expressions.md), controlling whether this command should execute.
        /// </summary>
        [ParameterAlias("if")]
        public StringParameter ConditionalExpression;

        [SerializeField] private PlaybackSpot playbackSpot = default;

        /// <summary>
        /// Creates new instance by parsing provided script text fragment representing body of the command.
        /// </summary>
        /// <param name="scriptName">Name of the script asset which contains the command.</param>
        /// <param name="lineIndex">Index of the line which contains the command.</param>
        /// <param name="inlineIndex">Index of the command inside the line (eg, when it's a part of <see cref="GenericTextScriptLine"/>).</param>
        /// <param name="commandBodyText">The command body text to parse (see remarks for the expected format).</param>
        /// <param name="errors">Parse errors (if any) or null when the parse has succeeded.</param>
        /// <remarks>
        /// Command body text is expected in the following format: `CommandId NamelessParamValue NamedParamKey:NamedParamValue`,
        /// where the nameless parameter is optional and there could be any amount of named parameter key-value pairs separated by spaces.
        /// </remarks>
        public static Command FromScriptText (string scriptName, int lineIndex, int inlineIndex, string commandBodyText, out string errors)
        {
            errors = null;

            var commandId = ExtractCommandId(commandBodyText, out errors);
            if (!string.IsNullOrEmpty(errors)) return null; 

            var commandType = ResolveCommandType(commandId);
            if (commandType is null)
            {
                errors = $"Command `{commandId}` not found in project's naninovel command types.";
                return null;
            }

            var command = Activator.CreateInstance(commandType) as Command;
            command.playbackSpot = new PlaybackSpot(scriptName, lineIndex, inlineIndex);

            var commandParameters = ExtractParameters(commandBodyText, out errors);
            if (!string.IsNullOrEmpty(errors)) return null;

            var paramFields = commandType.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => typeof(ICommandParameter).IsAssignableFrom(f.FieldType));
            var supportedParamIds = new List<string>(paramFields.Select(f => f.Name));
            foreach (var paramField in paramFields)
            {
                var paramRequired = paramField.GetCustomAttribute<RequiredParameterAttribute>() != null;
                var paramAlias = paramField.GetCustomAttribute<ParameterAliasAttribute>()?.Alias;
                if (paramAlias != null) supportedParamIds.Add(paramAlias);

                var paramId = paramAlias != null && commandParameters.ContainsKey(paramAlias) ? paramAlias : paramField.Name;
                if (!commandParameters.ContainsKey(paramId))
                {
                    if (paramRequired)
                        command.LogWarningWithPosition($"Command `{commandType.Name}` is missing `{paramId}` parameter.");
                    continue;
                }

                var parameter = Activator.CreateInstance(paramField.FieldType) as ICommandParameter;
                var paramValueText = commandParameters[paramId];
                parameter.SetValueFromScriptText(command.PlaybackSpot, paramValueText, out errors);
                if (!string.IsNullOrEmpty(errors)) return null;

                paramField.SetValue(command, parameter);
            }

            foreach (var paramId in commandParameters.Keys) // Check for unsupported parameters.
                if (!supportedParamIds.Any(id => id.EqualsFastIgnoreCase(paramId)))
                    command.LogWarningWithPosition($"Command `{commandType.Name}` has an unsupported `{(paramId == NamelessParameterAlias ? "nameless" : paramId)}` parameter.");

            return command;
        }

        /// <summary>
        /// Attempts to find a <see cref="Command"/> type based on the provided command alias or type name.
        /// </summary>
        public static Type ResolveCommandType (string commandId)
        {
            if (string.IsNullOrEmpty(commandId))
                return null;

            // First, try to resolve by key.
            CommandTypes.TryGetValue(commandId, out Type result);
            // If not found, look by type name (in case type name was requested for a command with a defined alias).
            if (result is null)
                result = CommandTypes.Values.FirstOrDefault(commandType => commandType.Name.EqualsFastIgnoreCase(commandId));
            return result;
        }

        /// <summary>
        /// Due to Unity's serialization design flaw (https://issuetracker.unity3d.com/product/unity/issues/guid/1206352)
        /// any constructor and field initalization logic should be repeated here. Don't use for anything else, as this will be removed when fixed.
        /// </summary>
        // TODO: Keep an eye on https://issuetracker.unity3d.com/issues/serializereference-non-serialized-initialized-fields-lose-their-values-when-entering-play-mode;
        // if won't work, consider performing Object.Instantiate() on each loaded script asset (in the editor only). To reproduce, play Script001 in the editor multiple times.
        // Delete ISerializationCallbackReceiver methods after this is fixed. 
        public virtual void OnAfterDeserialize () { }
        public void OnBeforeSerialize () { }

        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="cancellationToken">When cancellation is requested, the async routine should return ASAP and refrain from modifying state of the engine services or objects.</param>
        public abstract UniTask ExecuteAsync (CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs a message to the console; will include script name and line number of the command.
        /// </summary>
        public void LogWithPosition (string message, LogType logType = LogType.Log)
        {
            Script.LogWithPosition(PlaybackSpot, message, logType);
        }

        /// <summary>
        /// Logs a warning to the console; will include script name and line number of the command.
        /// </summary>
        public void LogWarningWithPosition (string message) => LogWithPosition(message, LogType.Warning);

        /// <summary>
        /// Logs an error to the console; will include script name and line number of the command.
        /// </summary>
        public void LogErrorWithPosition (string message) => LogWithPosition(message, LogType.Error);

        /// <summary>
        /// Tests whether the provided parameter is not null and has a value assigned.
        /// </summary>
        protected static bool Assigned (ICommandParameter parameter) => !(parameter is null) && parameter.HasValue;

        private static LiteralMap<Type> GetCommandTypes ()
        {
            var result = new LiteralMap<Type>();
            var commandTypes = ReflectionUtils.ExportedDomainTypes
                .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Command)));
            foreach (var commandType in commandTypes)
            {
                var commandKey = commandType.GetCustomAttributes(typeof(CommandAliasAttribute), false)
                    .FirstOrDefault() is CommandAliasAttribute tagAttribute && !string.IsNullOrEmpty(tagAttribute.Alias) ? tagAttribute.Alias : commandType.Name;
                if (result.ContainsKey(commandKey))
                {
                    Debug.LogWarning($"Failed to add `{commandType.Name}` (`{commandKey}` alias). Command with the same type name or alias already exists.");
                    continue;
                }
                else result.Add(commandKey, commandType);
            }
            return result;
        }

        private static string ExtractCommandId (string commandBodyText, out string errors)
        {
            errors = null;
            var commandId = commandBodyText.GetBefore(" ") ?? commandBodyText.GetBefore("\t") ?? commandBodyText.Trim();
            if (string.IsNullOrEmpty(commandId))
            {
                errors = "Failed to parse command ID.";
                return null;
            }
            return commandId;
        }

        /// <summary>
        /// Returns parameters of the command in `paramId -> paramValueText` map.
        /// </summary>
        private static LiteralMap<string> ExtractParameters (string commandBodyText, out string errors)
        {
            errors = null;
            var cmdParams = new LiteralMap<string>();

            var paramPairs = ExtractParamPairs(commandBodyText);
            if (paramPairs is null) return cmdParams; // No params in the line.

            foreach (var paramPair in paramPairs)
            {
                var paramName = string.Empty;
                var paramValue = string.Empty;
                if (ParamPairNameless(paramPair)) // Corner case for nameless params.
                {
                    if (cmdParams.ContainsKey(string.Empty))
                    {
                        errors = "There could be only one nameless parameter per command. Make sure there is no spaces in the parameter values; if you want to use spaces, wrap the value in double quotes (\").";
                        return cmdParams;
                    }
                    paramValue = paramPair;
                }
                else
                {
                    paramName = paramPair.GetBefore(ParameterAssignLiteral);
                    paramValue = paramPair.GetAfterFirst(ParameterAssignLiteral);
                }

                if (paramName is null || paramValue is null)
                {
                    errors = $"Failed to parse a `{paramPair}` named parameter.";
                    return cmdParams;
                }

                // Trim quotes in case parameter value is wrapped in them.
                if (paramValue.WrappedIn("\""))
                    paramValue = paramValue.Substring(1, paramValue.Length - 2);

                // Restore escaped quotes.
                paramValue = paramValue.Replace("\\\"", "\"");

                if (cmdParams.ContainsKey(paramName))
                {
                    errors = $"Dublicate parameter with `{paramName}` ID.";
                    continue;
                }
                else cmdParams.Add(paramName, paramValue);
            }

            return cmdParams;

            // The string doesn't contain assign literal, or it's within (non-escaped) quotes.
            bool ParamPairNameless (string paramPair)
            {
                if (!paramPair.Contains(ParameterAssignLiteral)) return true;

                var assignChar = ParameterAssignLiteral[0];
                var isInsideQuotes = false;
                bool IsQuotesAt (int index)
                {
                    var c = paramPair[index];
                    if (c != '"') return false;
                    if (index > 0 && paramPair[index - 1] == '\\') return false;
                    return true;
                }

                for (int i = 0; i < paramPair.Length; i++)
                {
                    if (IsQuotesAt(i))
                        isInsideQuotes = !isInsideQuotes;
                    if (isInsideQuotes) continue;

                    if (paramPair[i] == assignChar) return false;
                }

                return true;
            }

            // Capture whitespace and tabs, but ignore regions inside (non-escaped) quotes.
            List<string> ExtractParamPairs (string scriptLineText)
            {
                var paramStartIndex = scriptLineText.IndexOf(' ') + 1;
                if (paramStartIndex == 0) paramStartIndex = scriptLineText.IndexOf('\t') + 1; // Try tab.
                if (paramStartIndex == 0) return null; // No params in the line.

                var paramText = scriptLineText.Substring(paramStartIndex);
                var result = new List<string>();

                var captureStartIndex = -1;
                var isInsideQuotes = false;
                bool IsCapturing () => captureStartIndex >= 0;
                bool IsDelimeterChar (char c) => c == ' ' || c == '\t';
                bool IsQuotesAt (int index)
                {
                    var c = paramText[index];
                    if (c != '"') return false;
                    if (index > 0 && paramText[index - 1] == '\\') return false;
                    return true;
                }
                void StartCaptureAt (int index) => captureStartIndex = index;
                void FinishCaptureAt (int index)
                {
                    var paramPair = paramText.Substring(captureStartIndex, index - captureStartIndex + 1);
                    result.Add(paramPair);
                    captureStartIndex = -1;
                }

                for (int i = 0; i < paramText.Length; i++)
                {
                    var c = paramText[i];

                    if (!IsCapturing() && IsDelimeterChar(c)) continue;
                    if (!IsCapturing()) StartCaptureAt(i);

                    if (IsQuotesAt(i))
                        isInsideQuotes = !isInsideQuotes;
                    if (isInsideQuotes) continue;

                    if (IsDelimeterChar(c))
                    {
                        FinishCaptureAt(i - 1);
                        continue;
                    }

                    if (i == (paramText.Length - 1))
                        FinishCaptureAt(i);
                }

                return result;
            }
        }
    } 
}
