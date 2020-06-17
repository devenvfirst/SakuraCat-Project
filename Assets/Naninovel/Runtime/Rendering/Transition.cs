// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Describes a transition render effect.
    /// </summary>
    public readonly struct Transition
    {
        /// <summary>
        /// Name of the transition.
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Parameters of the transition.
        /// </summary>
        public Vector4 Parameters => custom ? parameters : TransitionUtils.GetDefaultParams(Name);
        /// <summary>
        /// Dissolve texture (mask) to use when <see cref="TransitionType.Custom"/>.
        /// </summary>
        public readonly Texture DissolveTexture;

        // Used to disinguish default instances.
        private readonly bool custom;
        private readonly Vector4 parameters;

        /// <summary>
        /// Creates a new instance with the provided transition name (case-insensitive) and default parameters.
        /// </summary>
        public Transition (string name, Texture dissolveTexture = default)
        {
            custom = true;
            Name = name;
            parameters = TransitionUtils.GetDefaultParams(name);
            DissolveTexture = dissolveTexture;
        }

        /// <summary>
        /// Creates a new instance with the provided transition name (case-insensitive) and parameters.
        /// </summary>
        public Transition (string name, Vector4 parameters, Texture dissolveTexture = default)
        {
            custom = true;
            Name = name;
            this.parameters = parameters;
            DissolveTexture = dissolveTexture;
        }
    }
}
