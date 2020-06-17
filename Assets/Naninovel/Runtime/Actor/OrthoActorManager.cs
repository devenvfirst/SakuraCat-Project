// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages <typeparamref name="TActor"/> objects in orthographic scene space.
    /// </summary>
    public abstract class OrthoActorManager<TActor, TState, TMeta, TConfig> : ActorManager<TActor, TState, TMeta, TConfig>
        where TActor : IActor
        where TState : ActorState<TActor>, new()
        where TMeta : OrthoActorMetadata
        where TConfig : OrthoActorManagerConfiguration<TMeta>
    {
        /// <summary>
        /// Scene origin point position in world space.
        /// </summary>
        protected Vector2 GlobalSceneOrigin => CameraConfiguration.SceneToWorldSpace(Configuration.SceneOrigin);
        /// <summary>
        /// Initial Z-axis offset distance (depth) from actors to the camera to set when the actors are created.
        /// </summary>
        protected float ZOffset => Configuration.ZOffset;
        /// <summary>
        /// Z-position of the actor closest to the camera.
        /// </summary>
        protected float TopmostZPosition => ZOffset - ManagedActors.Count * Configuration.ZStep;
        protected CameraConfiguration CameraConfiguration { get; private set; }

        public OrthoActorManager (TConfig config, CameraConfiguration cameraConfig)
            : base(config)
        {
            CameraConfiguration = cameraConfig;
        }

        /// <summary>
        /// Changes provided actor y position so that it's bottom edge is alligned with the bottom of the screen.
        /// </summary>
        protected void MoveActorToBottom (TActor actor)
        {
            var metadata = Configuration.GetMetadataOrDefault(actor.Id);
            var bottomY = (metadata.Pivot.y * actor.Scale.y) / metadata.PixelsPerUnit - CameraConfiguration.MaxOrthoSize;
            actor.ChangePositionY(bottomY);
        }

        protected override async UniTask<TActor> ConstructActorAsync (string actorId)
        {
            var actor = await base.ConstructActorAsync(actorId);

            // When adding a new actor place it at the topmost z-position.
            actor.ChangePositionZ(TopmostZPosition);

            return actor;
        }
    }
}
