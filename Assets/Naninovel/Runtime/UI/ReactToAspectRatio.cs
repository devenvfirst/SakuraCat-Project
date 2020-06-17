// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;
using UnityEngine.Events;

namespace Naninovel
{
    /// <summary>
    /// Allows listening for events when display aspect ratio goes below or above specified threshold.
    /// </summary>
    public class ReactToAspectRatio : MonoBehaviour
    {
        [System.Serializable]
        private class ThresholdReachedEvent : UnityEvent<bool> { }

        [Tooltip("When aspect ratio (width divided by height) goes above or below the value, the event will be invoked.")]
        [SerializeField] private float aspectThreshold = 1f;
        [Tooltip("Invoked when aspect ratio (width divided by height) is changed and become either equal or above (true) or below (false) specified threshold.")]
        [SerializeField] private ThresholdReachedEvent onThresholdReached = default;

        private ICameraManager cameraManager;
        private float lastRatio;

        private void Awake ()
        {
            cameraManager = Engine.GetService<ICameraManager>();
            HandleAspectChanged(cameraManager.GetAspectRatio());
            cameraManager.OnAspectChanged += HandleAspectChanged;
        }

        private void OnDestroy ()
        {
            cameraManager.OnAspectChanged -= HandleAspectChanged;
        }

        private void HandleAspectChanged (float ratio)
        {
            if (lastRatio == ratio) return;

            onThresholdReached?.Invoke(ratio >= aspectThreshold);

            lastRatio = ratio;
        }
    }
}
