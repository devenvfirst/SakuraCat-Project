// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class ScriptPlayerConfiguration : Configuration
    {
        [Tooltip("Time scale to use when in skip (fast-forward) mode.")]
        public float SkipTimeScale = 10f;
        [Tooltip("Minimum seconds to wait before executing next command while in auto play mode.")]
        public float MinAutoPlayDelay = 3f;
        [Tooltip("Whether to show player debug window on engine initialization.")]
        public bool ShowDebugOnInit = false;
    }
}
