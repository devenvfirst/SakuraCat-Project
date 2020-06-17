// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Naninovel
{
    [Serializable]
    public struct SpawnedObjectState : ISerializationCallbackReceiver, IEquatable<SpawnedObjectState>
    {
        public string Path { get; private set; }
        public string[] Params { get; private set; }

        [SerializeField] private string path;
        [SerializeField] private string paramsString;

        public SpawnedObjectState (string path, string[] @params)
        {
            this.path = default;
            paramsString = default;
            Path = path;
            Params = @params;
        }

        public void OnBeforeSerialize ()
        {
            path = Path;
            paramsString = (Params is null || Params.Length == 0) ? null : string.Join(",", Params);
        }

        public void OnAfterDeserialize ()
        {
            Path = path;
            Params = string.IsNullOrEmpty(paramsString) ? null : paramsString.Split(',');
        }

        public override bool Equals (object obj)
        {
            return obj is SpawnedObjectState state && Equals(state);
        }

        public bool Equals (SpawnedObjectState other)
        {
            return Path == other.Path &&
                   EqualityComparer<string[]>.Default.Equals(Params, other.Params);
        }

        public override int GetHashCode ()
        {
            var hashCode = 289869881;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Params);
            return hashCode;
        }

        public static bool operator == (SpawnedObjectState left, SpawnedObjectState right)
        {
            return left.Equals(right);
        }

        public static bool operator != (SpawnedObjectState left, SpawnedObjectState right)
        {
            return !(left == right);
        }
    }
}
