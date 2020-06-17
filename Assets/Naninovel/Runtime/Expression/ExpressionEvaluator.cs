// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.NCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows parsing and evaluating mathematical and logical expressions.
    /// </summary>
    public static class ExpressionEvaluator
    {
        [ExpressionFunctions]
        public static class Functions
        {
            [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
            public sealed class DocAttribute : Attribute
            {
                public string Description { get; }
                public string Example { get; }

                public DocAttribute (string description, string example = null)
                {
                    Description = description;
                    Example = example;
                }
            }

            [Doc("Return a random float number between min [inclusive] and max [inclusive].", "Random(0.1, 0.85)")]
            public static float Random (double min, double max) => UnityEngine.Random.Range((float)min, (float)max);
            [Doc("Return a random integer number between min [inclusive] and max [inclusive].", "Random(0, 100)")]
            public static int Random (int min, int max) => UnityEngine.Random.Range(min, max + 1);
            [Doc("Return a string chosen from one of the provided strings.", "Random(\"Foo\", \"Bar\", \"Foobar\")")]
            public static string Random (params string[] args) => args.Random();
            [Doc("Return a float number in 0.0 to 1.0 range, representing how many unique commands were ever executed compared to the total number of commands in all the available naninovel scripts. 1.0 means the player had `read through` or `seen` all the available game content. Make sure to enable `Count Total Commands` in the script configuration menu before using this function.", "CalculateProgress()")]
            public static float CalculateProgress ()
            {
                var scriptMngr = Engine.GetService<IScriptManager>();
                var player = Engine.GetService<IScriptPlayer>();
                if (scriptMngr.TotalCommandsCount == 0)
                {
                    Debug.LogWarning("`CalculateProgress` script expression function were used, while to total number of script commands is zero. You've most likely disabled `UpdateActionCountOnInit` in the script player configuration menu or didn't add any naninovel sctipts to the project resources.");
                    return 0;
                }
                return player.PlayedCommandsCount / (float)scriptMngr.TotalCommandsCount;
            }
        }

        public const string ManagedTextScriptCategory = "Script";
        public const string ManagedTextKeyPrefix = "t_";

        private static readonly List<MethodInfo> functions = new List<MethodInfo>();

        static ExpressionEvaluator ()
        {
            functions.AddRange(typeof(Mathf).GetMethods(BindingFlags.Public | BindingFlags.Static));
            functions.AddRange(typeof(Math).GetMethods(BindingFlags.Public | BindingFlags.Static));

            var customFunctions = ReflectionUtils.ExportedDomainTypes
                .Where(t => t.IsDefined(typeof(ExpressionFunctionsAttribute)))
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static)).ToList();
            functions.AddRange(customFunctions);
        }

        public static TResult Evaluate<TResult> (string expressionString, Action<string> onError = null)
        {
            var resultType = typeof(TResult);
            return (TResult)Evaluate(expressionString, resultType, onError);
        }

        public static object Evaluate (string expressionString, Type resultType, Action<string> onError = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expressionString))
                {
                    onError?.Invoke("Expression is missing.");
                    return default;
                }

                // Escape all the un-escaped single quotes.
                expressionString = Regex.Replace(expressionString, @"(?<!\\)'", @"\'");
                // Replace un-escaped double quotes with single quotes.
                expressionString = Regex.Replace(expressionString, @"(?<!\\)""", @"'");

                var expression = new Expression(expressionString, EvaluateOptions.IgnoreCase | EvaluateOptions.MatchStringsOrdinal);
                expression.EvaluateParameter += EvaluateExpressionParameter;
                expression.EvaluateFunction += EvaluateExpressionFunction;

                if (expression.HasErrors())
                {
                    onError?.Invoke($"Expression `{expressionString}` syntax error: {expression.Error}");
                    return default;
                }

                var resultObj = expression.Evaluate();
                if (resultObj is null)
                {
                    onError?.Invoke($"Expression `{expressionString}` result is null.");
                    return default;
                }

                return Convert.ChangeType(resultObj, resultType, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Failed to evaluate expression `{expressionString}`. Error message: {e.Message}");
                return default;
            }
        }

        private static void EvaluateExpressionParameter (string name, ParameterArgs args)
        {
            if (name.StartsWith(ManagedTextKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var textManager = Engine.GetService<ITextManager>();
                var managedTextValue = textManager.GetRecordValue(name, ManagedTextScriptCategory);
                if (string.IsNullOrEmpty(managedTextValue))
                    Debug.LogWarning($"Failed to find a managed text value of `{name}`. Make sure the corresponding record exists in a `{ManagedTextScriptCategory}` managed text document.");
                args.Result = managedTextValue;
                return;
            }

            var variableManager = Engine.GetService<ICustomVariableManager>();
            if (!variableManager.VariableExists(name))
                Debug.LogWarning($"Custom variable `{name}` doesn't exist, but its value is requested in a script expression; this could lead to evaluation errors. Make sure to initialize variables with `@set` command or via `Custom Variables` configuration menu before using them.");
            var strValue = variableManager.GetVariableValue(name) ?? string.Empty;
            args.Result = CustomVariablesConfiguration.ParseVariableValue(strValue);
        }

        private static void EvaluateExpressionFunction (string name, FunctionArgs args)
        {
            foreach (var methodInfo in functions)
            {
                // Check name equality.
                if (!methodInfo.Name.EqualsFastIgnoreCase(name)) continue;

                var functionParams = args.Parameters.Select(p => p.Evaluate()).ToArray();
                var methodParams = methodInfo.GetParameters();

                // Handle functions with single `params` argument.
                if (methodParams.Length == 1 && methodParams[0].IsDefined(typeof(ParamArrayAttribute)) &&
                    functionParams.All(p => p.GetType() == methodParams[0].ParameterType.GetElementType()))
                {
                    var elementType = methodParams[0].ParameterType.GetElementType();
                    var paramsValue = Array.CreateInstance(elementType, functionParams.Length);
                    Array.Copy(functionParams, paramsValue, functionParams.Length);
                    args.Result = methodInfo.Invoke(null, new object[] { paramsValue });
                    break;
                }

                // Check argument count equality.
                if (methodParams.Length != functionParams.Length) continue;

                // Check argument type and order equality.
                var paramTypeCheckPassed = true;
                for (int i = 0; i < methodParams.Length; i++)
                    if (methodParams[i].ParameterType != functionParams[i].GetType()) { paramTypeCheckPassed = false; break; }
                if (!paramTypeCheckPassed) continue;

                args.Result = methodInfo.Invoke(null, functionParams);
                break;
            }
        }
    }
}
