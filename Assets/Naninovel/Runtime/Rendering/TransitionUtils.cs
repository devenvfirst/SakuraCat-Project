// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    public static class TransitionUtils
    {
        private const string keywordPrefix = "NANINOVEL_TRANSITION_";

        private static readonly Dictionary<string, Vector4> nameToDefaultParamsMap = new Dictionary<string, Vector4> (StringComparer.OrdinalIgnoreCase) {
            [TransitionType.Crossfade] = Vector4.zero,
            [TransitionType.BandedSwirl] = new Vector4(5, 10),
            [TransitionType.Blinds] = new Vector4(6, 0),
            [TransitionType.CircleReveal] = new Vector4(.25f, 0),
            [TransitionType.CircleStretch] = Vector4.zero,
            [TransitionType.CloudReveal] = Vector4.zero,
            [TransitionType.Crumble] = Vector4.zero,
            [TransitionType.Dissolve] = new Vector4(99999, 0),
            [TransitionType.DropFade] = Vector4.zero,
            [TransitionType.LineReveal] = new Vector4(.025f, .5f, .5f),
            [TransitionType.Pixelate] = Vector4.zero,
            [TransitionType.RadialBlur] = Vector4.zero,
            [TransitionType.RadialWiggle] = Vector4.zero,
            [TransitionType.RandomCircleReveal] = Vector4.zero,
            [TransitionType.Ripple] = new Vector4(20f, 10f, .05f),
            [TransitionType.RotateCrumble] = Vector4.zero,
            [TransitionType.Saturate] = Vector4.zero,
            [TransitionType.Shrink] = new Vector4(200, 0),
            [TransitionType.SlideIn] = new Vector4(1, 0),
            [TransitionType.SwirlGrid] = new Vector4(15, 10),
            [TransitionType.Swirl] = new Vector4(15, 0),
            [TransitionType.Water] = Vector4.zero,
            [TransitionType.Waterfall] = Vector4.zero,
            [TransitionType.Wave] = new Vector4(.1f, 14, 20),
            [TransitionType.Custom] = Vector4.zero
        };

        /// <summary>
        /// Converts provided transition name to corresponding shader keyword.
        /// Transition effect names are case-insensitive.
        /// </summary>
        public static string ToShaderKeyword (string transition)
        {
            return string.Concat(keywordPrefix, transition.ToUpperInvariant());
        }

        /// <summary>
        /// Attempts to find default transition parameters for transition effect with the provided name;
        /// returns <see cref="Vector4.zero"/> when not found. Transition effect names are case-insensitive.
        /// </summary>
        public static Vector4 GetDefaultParams (string transition)
        {
            return nameToDefaultParamsMap.TryGetValue(transition, out var result) ? result : Vector4.zero;
        }

        /// <summary>
        /// Attempts to find which transition effect is currently enabled in the provided material by checking enabled keywords;
        /// returns <see cref="TransitionType.Crossfade"/> when no transition keyword is enabled or found.
        /// </summary>
        public static string GetEnabled (Material material)
        {
            for (int i = 0; i < material.shaderKeywords.Length; i++)
                if (material.shaderKeywords[i].StartsWith(keywordPrefix) && material.IsKeywordEnabled(material.shaderKeywords[i]))
                    return material.shaderKeywords[i].GetAfter(keywordPrefix);
            return TransitionType.Crossfade; // Crossfade is executed by default when no keywords enabled.
        }

        /// <summary>
        /// Enables a shader keyword corresponding to transition effect with the provided name in the provided material.
        /// Transition effect names are case-insensitive.
        /// </summary>
        public static void EnableKeyword (Material material, string transition)
        {
            for (int i = 0; i < material.shaderKeywords.Length; i++)
                if (material.shaderKeywords[i].StartsWith(keywordPrefix) && material.IsKeywordEnabled(material.shaderKeywords[i]))
                    material.DisableKeyword(material.shaderKeywords[i]);

            // Crossfade is executed when no transition keywords enabled.
            if (transition.EqualsFastIgnoreCase(TransitionType.Crossfade)) return;

            var keyword = ToShaderKeyword(transition);
            material.EnableKeyword(keyword);
        }
    }
}
