// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents data required to construct and initialize a <see cref="IBackgroundActor"/>.
    /// </summary>
    [System.Serializable]
    public class BackgroundMetadata : OrthoActorMetadata
    {
        [System.Serializable]
        public class Map : ActorMetadataMap<BackgroundMetadata> { }
        [System.Serializable]
        public class Pose : ActorPose<BackgroundState> { }

        [Tooltip("Named states (poses) of the background; pose name can be used as appearance in `@back` commands to apply associated state.")]
        public List<Pose> Poses = new List<Pose>();

        public BackgroundMetadata ()
        {
            Implementation = typeof(SpriteBackground).AssemblyQualifiedName;
            Loader = new ResourceLoaderConfiguration { PathPrefix = BackgroundsConfiguration.DefaultPathPrefix };
            Pivot = new Vector2(.5f, .5f);
        }

        public override TState GetPoseOrNull<TState> (string poseName) => Poses.FirstOrDefault(p => p.Name == poseName)?.ActorState as TState;
    }
}
