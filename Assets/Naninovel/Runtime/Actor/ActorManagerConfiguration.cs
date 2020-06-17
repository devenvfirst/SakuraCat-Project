// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Asset used to configure <see cref="IActorManager"/> services.
    /// </summary>
    [System.Serializable]
    public abstract class ActorManagerConfiguration : Configuration
    {
        [Tooltip("Eeasing function to use by default for all the actor modification animations (changing appearance, position, tint, etc).")]
        public EasingType DefaultEasing = EasingType.Linear;
        [Tooltip("Whether to automatically reveal (show) an actor when executing modification commands.")]
        public bool AutoShowOnModify = true;

        /// <summary>
        /// Attemtps to retrieve metadata of an actor with the provided ID;
        /// when not found, will return a defualt metadata.
        /// </summary>
        public ActorMetadata GetMetadataOrDefault (string actorId) => GetMetadataNonGeneric(actorId);

        protected abstract ActorMetadata GetMetadataNonGeneric (string actorId);
    }

    /// <summary>
    /// Asset used to configure <see cref="IActorManager"/> services.
    /// </summary>
    /// <typeparam name="TMeta">Type of actor metadata configured service operates with.</typeparam>
    public abstract class ActorManagerConfiguration<TMeta> : ActorManagerConfiguration
        where TMeta : ActorMetadata
    {
        protected abstract TMeta DefaultActorMetadata { get; }
        protected abstract ActorMetadataMap<TMeta> ActorMetadataMap { get; }

        /// <summary>
        /// Attemtps to retrieve metadata of an actor with the provided ID;
        /// when not found, will return a defualt metadata.
        /// </summary>
        public new TMeta GetMetadataOrDefault (string actorId)
        {
            return ActorMetadataMap.ContainsId(actorId) ? ActorMetadataMap[actorId] : DefaultActorMetadata;
        }

        protected override ActorMetadata GetMetadataNonGeneric (string actorId) => GetMetadataOrDefault(actorId);
    }
}
