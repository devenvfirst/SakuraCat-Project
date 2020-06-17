// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using UniRx.Async;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <inheritdoc cref="IInputSampler"/>
    public class InputSampler : IInputSampler
    {
        public event Action OnStart;
        public event Action OnEnd;

        public InputBinding Binding { get; }
        public bool Active => Value != 0;
        public float Value { get; private set; }
        public bool StartedDuringFrame => Active && Time.frameCount == lastActiveFrame;
        public bool EndedDuringFrame => !Active && Time.frameCount == lastActiveFrame;

        private readonly InputConfiguration config;
        private readonly HashSet<GameObject> objectTriggers;
        private readonly float touchCooldown;
        private UniTaskCompletionSource<bool> onInputTCS;
        private UniTaskCompletionSource<object> onInputStartTCS, onInputEndTCS;
        private CancellationTokenSource onInputStartCTS, onInputEndCTS;
        private float lastTouchTime;
        private int lastActiveFrame;

        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        private readonly UnityEngine.InputSystem.InputAction inputAction;
        #endif

        /// <param name="config">Input manager configuration asset.</param>
        /// <param name="binding">Binding to trigger input.</param>
        /// <param name="objectTriggers">Objects to trigger input.</param>
        /// <param name="touchCooldown">Delay for detecting touch input state changes.</param>
        public InputSampler (InputConfiguration config, InputBinding binding, IEnumerable<GameObject> objectTriggers, float touchCooldown)
        {
            Binding = binding;
            this.config = config;
            this.objectTriggers = objectTriggers != null ? new HashSet<GameObject>(objectTriggers) : new HashSet<GameObject>();
            this.touchCooldown = touchCooldown;

            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            if (ObjectUtils.IsValid(config.InputActions))
                inputAction = config.InputActions.FindActionMap("Naninovel")?.FindAction(binding.Name);
            inputAction?.Enable();
            #endif
        }

        public void AddObjectTrigger (GameObject obj) => objectTriggers.Add(obj);

        public void RemoveObjectTrigger (GameObject obj) => objectTriggers.Remove(obj);

        public async UniTask<bool> WaitForInputAsync ()
        {
            if (onInputTCS is null) onInputTCS = new UniTaskCompletionSource<bool>();
            return await onInputTCS.Task;
        }

        public async UniTask WaitForInputStartAsync ()
        {
            if (onInputStartTCS is null) onInputStartTCS = new UniTaskCompletionSource<object>();
            await onInputStartTCS.Task;
        }

        public async UniTask WaitForInputEndAsync ()
        {
            if (onInputEndTCS is null) onInputEndTCS = new UniTaskCompletionSource<object>();
            await onInputEndTCS.Task;
        }

        public CancellationToken GetInputStartCancellationToken ()
        {
            if (onInputStartCTS is null) onInputStartCTS = new CancellationTokenSource();
            return onInputStartCTS.Token;
        }

        public CancellationToken GetInputEndCancellationToken ()
        {
            if (onInputEndCTS is null) onInputEndCTS = new CancellationTokenSource();
            return onInputEndCTS.Token;
        }

        /// <summary>
        /// Performs the sampling, updating the input status; expected to be invoked on each render loop update.
        /// </summary>
        public void SampleInput ()
        {
            #if ENABLE_LEGACY_INPUT_MANAGER
            if (config.ProcessLegacyBindings && Binding.Keys?.Count > 0)
                foreach (var key in Binding.Keys)
                {
                    if (Input.GetKeyDown(key)) SetInputValue(1);
                    if (Input.GetKeyUp(key)) SetInputValue(0);
                }

            if (config.ProcessLegacyBindings && Binding.Axes?.Count > 0)
            {
                var maxValue = 0f;
                foreach (var axis in Binding.Axes)
                {
                    var axisValue = axis.Sample();
                    if (Mathf.Abs(axisValue) > Mathf.Abs(maxValue))
                        maxValue = axisValue;
                }
                if (maxValue != Value)
                    SetInputValue(maxValue);
            }

            if (Input.touchSupported && Binding.Swipes?.Count > 0)
            {
                var swipeRegistered = false;
                foreach (var swipe in Binding.Swipes)
                    if (swipe.Sample()) { swipeRegistered = true; break; }
                if (swipeRegistered != Active) SetInputValue(swipeRegistered ? 1 : 0);
            }

            if (objectTriggers.Count > 0)
            {
                var touchBegan = Input.touchCount > 0
                    && Input.GetTouch(0).phase == TouchPhase.Began
                    && (Time.time - lastTouchTime) > touchCooldown;
                if (touchBegan) lastTouchTime = Time.time;
                var clickedDown = Input.GetMouseButtonDown(0);
                if (clickedDown || touchBegan)
                {
                    var hoveredObject = EventSystem.current.GetHoveredGameObject();
                    if (hoveredObject && objectTriggers.Contains(hoveredObject))
                        SetInputValue(1f);
                }

                var touchEnded = Input.touchCount > 0
                    && Input.GetTouch(0).phase == TouchPhase.Ended;
                var clickedUp = Input.GetMouseButtonUp(0);
                if (touchEnded || clickedUp) SetInputValue(0f);
            }
            #endif

            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            if (inputAction != null)
            {
                if (inputAction.type == UnityEngine.InputSystem.InputActionType.Value)
                {
                    var value = inputAction.ReadValue<float>();
                    SetInputValue(value);
                }
                else SetInputValue(inputAction.triggered ? 1 : 0);
            }
            #endif
        }

        private void SetInputValue (float value)
        {
            Value = value;
            lastActiveFrame = Time.frameCount;

            onInputTCS?.TrySetResult(Active);
            onInputTCS = null;
            if (Active)
            {
                onInputStartTCS?.TrySetResult(null);
                onInputStartTCS = null;
                onInputStartCTS?.Cancel();
                onInputStartCTS?.Dispose();
                onInputStartCTS = null;
            }
            else
            {
                onInputEndTCS?.TrySetResult(null);
                onInputEndTCS = null;
                onInputEndCTS?.Cancel();
                onInputEndCTS?.Dispose();
                onInputEndCTS = null;
            }
           
            if (Active) OnStart?.Invoke();
            else OnEnd?.Invoke();
        }
    }
}
