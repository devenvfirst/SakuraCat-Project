// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ICustomVariableManager"/>
    [InitializeAtRuntime]
    public class CustomVariableManager : IStatefulService<GameStateMap>, IStatefulService<GlobalStateMap>, ICustomVariableManager
    {
        [System.Serializable]
        public class GlobalState
        {
            public SerializableLiteralStringMap GlobalVariableMap;
        }

        [System.Serializable]
        public class GameState
        {
            public SerializableLiteralStringMap LocalVariableMap;
        }

        public event Action<CustomVariableUpdatedArgs> OnVariableUpdated;

        public CustomVariablesConfiguration Configuration { get; }

        private readonly SerializableLiteralStringMap globalVariableMap;
        private readonly SerializableLiteralStringMap localVariableMap;

        public CustomVariableManager (CustomVariablesConfiguration config)
        {
            Configuration = config;
            globalVariableMap = new SerializableLiteralStringMap();
            localVariableMap = new SerializableLiteralStringMap();
        }

        public UniTask InitializeServiceAsync () => UniTask.CompletedTask;

        public void ResetService ()
        {
            ResetLocalVariables();
        }

        public void DestroyService () { }

        public void SaveServiceState (GlobalStateMap stateMap)
        {
            var state = new GlobalState {
                GlobalVariableMap = new SerializableLiteralStringMap(globalVariableMap)
            };
            stateMap.SetState(state);
        }

        public UniTask LoadServiceStateAsync (GlobalStateMap stateMap)
        {
            ResetGlobalVariables();

            var state = stateMap.GetState<GlobalState>();
            if (state is null) return UniTask.CompletedTask;

            foreach (var kv in state.GlobalVariableMap)
                globalVariableMap[kv.Key] = kv.Value;
            return UniTask.CompletedTask;
        }

        public void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState {
                LocalVariableMap = new SerializableLiteralStringMap(localVariableMap)
            };
            stateMap.SetState(state);
        }

        public UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            ResetLocalVariables();

            var state = stateMap.GetState<GameState>();
            if (state is null) return UniTask.CompletedTask;

            foreach (var kv in state.LocalVariableMap)
                localVariableMap[kv.Key] = kv.Value;
            return UniTask.CompletedTask;
        }

        public bool VariableExists (string name) => CustomVariablesConfiguration.IsGlobalVariable(name) ? globalVariableMap.ContainsKey(name) : localVariableMap.ContainsKey(name);

        public string GetVariableValue (string name)
        {
            if (!VariableExists(name)) return null;
            return CustomVariablesConfiguration.IsGlobalVariable(name) ? globalVariableMap[name] : localVariableMap[name];
        }

        public IEnumerable<CustomVariable> GetAllVariables ()
        {
            var result = new List<CustomVariable>();
            foreach (var kv in globalVariableMap)
                result.Add(new CustomVariable(kv.Key, kv.Value));
            foreach (var kv in localVariableMap)
                result.Add(new CustomVariable(kv.Key, kv.Value));
            return result;
        }

        public void SetVariableValue (string name, string value)
        {
            var isGlobal = CustomVariablesConfiguration.IsGlobalVariable(name);
            var initialValue = default(string);

            if (isGlobal)
            {
                globalVariableMap.TryGetValue(name, out initialValue);
                globalVariableMap[name] = value;
            }
            else
            {
                localVariableMap.TryGetValue(name, out initialValue);
                localVariableMap[name] = value;
            }

            if (initialValue != value)
                OnVariableUpdated?.Invoke(new CustomVariableUpdatedArgs(name, value, initialValue));
        }

        public void ResetLocalVariables ()
        {
            localVariableMap?.Clear();

            foreach (var varData in Configuration.PredefinedVariables)
            {
                if (CustomVariablesConfiguration.IsGlobalVariable(varData.Name)) continue;
                var value = ExpressionEvaluator.Evaluate<string>(varData.Value, e => LogInitializeVarError(varData.Name, varData.Value, e));
                SetVariableValue(varData.Name, value);
            }
        }

        public void ResetGlobalVariables ()
        {
            globalVariableMap?.Clear();

            foreach (var varData in Configuration.PredefinedVariables)
            {
                if (!CustomVariablesConfiguration.IsGlobalVariable(varData.Name)) continue;
                var value = ExpressionEvaluator.Evaluate<string>(varData.Value, e => LogInitializeVarError(varData.Name, varData.Value, e));
                SetVariableValue(varData.Name, value);
            }
        }

        private void LogInitializeVarError (string varName, string expr, string error) => Debug.LogWarning($"Failed to initialize `{varName}` varaible with `{expr}` expression: {error}");
    }
}
