// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.UI;
using System.Collections.Generic;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <inheritdoc cref="IInputManager"/>
    [InitializeAtRuntime]
    public class InputManager : IStatefulService<GameStateMap>, IInputManager
    {
        [System.Serializable]
        public class GameState
        {
            public bool ProcessInput = true;
        }

        public InputConfiguration Configuration { get; }
        public bool ProcessInput { get; set; } = true;

        private readonly Dictionary<string, InputSampler> samplersMap = new Dictionary<string, InputSampler>(System.StringComparer.Ordinal);
        private readonly Dictionary<IManagedUI, string[]> blockingUIs = new Dictionary<IManagedUI, string[]>();
        private readonly HashSet<string> blockedSamplers = new HashSet<string>();
        private readonly IEngineBehaviour engineBehaviour;
        private GameObject gameObject;

        public InputManager (InputConfiguration config, IEngineBehaviour engineBehaviour)
        {
            Configuration = config;
            this.engineBehaviour = engineBehaviour;
        }

        public async UniTask InitializeServiceAsync ()
        {
            foreach (var binding in Configuration.Bindings)
            {
                var sampler = new InputSampler(Configuration, binding, null, Configuration.TouchFrequencyLimit);
                samplersMap[binding.Name] = sampler;
            }

            gameObject = Engine.CreateObject("InputManager");

            if (Configuration.SpawnEventSystem)
            {
                if (ObjectUtils.IsValid(Configuration.CustomEventSystem))
                    Engine.Instantiate(Configuration.CustomEventSystem).transform.SetParent(gameObject.transform, false);
                else gameObject.AddComponent<EventSystem>();
            }

            if (Configuration.SpawnInputModule)
            {
                if (ObjectUtils.IsValid(Configuration.CustomInputModule))
                    Engine.Instantiate(Configuration.CustomInputModule).transform.SetParent(gameObject.transform, false);
                else
                { 
                    #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
                    var inputModule = gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    #else
                    var inputModule = gameObject.AddComponent<StandaloneInputModule>();
                    #endif
                    await AsyncUtils.WaitEndOfFrame;
                    inputModule.enabled = false; // Otherwise it stops processing UI events when using new input system.
                    inputModule.enabled = true;
                }
            }

            engineBehaviour.OnBehaviourUpdate += SampleInput;
        }

        public void ResetService () { }

        public void DestroyService ()
        {
            engineBehaviour.OnBehaviourUpdate -= SampleInput;
            if (gameObject) Object.Destroy(gameObject);
        }

        public void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState() {
                ProcessInput = ProcessInput
            };
            stateMap.SetState(state);
        }

        public UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            ProcessInput = state?.ProcessInput ?? true;
            return UniTask.CompletedTask;
        }

        public IInputSampler GetSampler (string bindingName)
        {
            if (!samplersMap.ContainsKey(bindingName)) return null;
            return samplersMap[bindingName];
        }

        public void AddBlockingUI (IManagedUI ui, params string[] allowedSamplers)
        {
            if (blockingUIs.ContainsKey(ui)) return;
            blockingUIs.Add(ui, allowedSamplers);
            ui.OnVisibilityChanged += HandleBlockingUIVisibilityChanged;
            HandleBlockingUIVisibilityChanged(ui.Visible);
        }

        public void RemoveBlockingUI (IManagedUI ui)
        {
            if (!blockingUIs.ContainsKey(ui)) return;
            blockingUIs.Remove(ui);
            ui.OnVisibilityChanged -= HandleBlockingUIVisibilityChanged;
            HandleBlockingUIVisibilityChanged(ui.Visible);
        }

        private void HandleBlockingUIVisibilityChanged (bool isVisible)
        {
            // If any of the blocking UIs are visible, all the samplers should be blocked,
            // except ones that are explicitly allowed by ALL the visible blocking UIs.
            
            // 1. Find the allowed samplers first; start with clearing the set.
            blockedSamplers.Clear();
            // 2. Store all the existing samplers.
            blockedSamplers.UnionWith(samplersMap.Keys);
            // 3. Remove samplers that are not allowed by any of the visible blocking UIs.
            foreach (var kv in blockingUIs)
                if (kv.Key.Visible)
                    blockedSamplers.IntersectWith(kv.Value);
            // 4. This will filter-out the samplers contained in both collections,
            // effectively storing only the non-allowed (blocked) ones in the set.
            blockedSamplers.SymmetricExceptWith(samplersMap.Keys);
        }

        private void SampleInput ()
        {
            if (!ProcessInput) return;

            foreach (var kv in samplersMap)
                if (!blockedSamplers.Contains(kv.Key) || kv.Value.Binding.AlwaysProcess)
                    kv.Value.SampleInput();
        }
    } 
}
