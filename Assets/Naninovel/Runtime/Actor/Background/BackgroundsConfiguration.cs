// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class BackgroundsConfiguration : OrthoActorManagerConfiguration<BackgroundMetadata>
    {
        /// <summary>
        /// ID of the background actor used by default.
        /// </summary>
        public const string MainActorId = "MainBackground";
        public const string DefaultPathPrefix = "Backgrounds";

        protected override BackgroundMetadata DefaultActorMetadata => DefaultMetadata;
        protected override ActorMetadataMap<BackgroundMetadata> ActorMetadataMap => Metadata;

        [Tooltip("Metadata to use by default when creating background actors and custom metadata for the created actor ID doesn't exist.")]
        public BackgroundMetadata DefaultMetadata = new BackgroundMetadata();
        [Tooltip("Metadata to use when creating background actors with specific IDs.")]
        public BackgroundMetadata.Map Metadata = new BackgroundMetadata.Map {
            [MainActorId] = new BackgroundMetadata()
        };
    }
}
