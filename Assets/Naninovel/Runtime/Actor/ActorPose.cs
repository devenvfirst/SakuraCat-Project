// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents a named state of an actor.
    /// </summary>
    [System.Serializable]
    public class ActorPose<TState> : IEquatable<ActorPose<TState>> 
        where TState : ActorState
    {
        /// <summary>
        /// Name (identifier) of the pose.
        /// </summary>
        public string Name => name;
        /// <summary>
        /// Actor state associated with the pose.
        /// </summary>
        public TState ActorState => actorState;

        [SerializeField] private string name = default;
        [SerializeField] private TState actorState = default;

        public override bool Equals (object obj)
        {
            return Equals(obj as ActorPose<TState>);
        }

        public bool Equals (ActorPose<TState> other)
        {
            return other != null &&
                   name == other.name &&
                   EqualityComparer<TState>.Default.Equals(actorState, other.actorState);
        }

        public override int GetHashCode ()
        {
            var hashCode = -753067675;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
            hashCode = hashCode * -1521134295 + EqualityComparer<TState>.Default.GetHashCode(actorState);
            return hashCode;
        }

        public static bool operator == (ActorPose<TState> left, ActorPose<TState> right)
        {
            return EqualityComparer<ActorPose<TState>>.Default.Equals(left, right);
        }

        public static bool operator != (ActorPose<TState> left, ActorPose<TState> right)
        {
            return !(left == right);
        }
    }
}
