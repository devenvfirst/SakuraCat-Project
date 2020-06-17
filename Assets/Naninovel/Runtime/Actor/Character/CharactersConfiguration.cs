// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class CharactersConfiguration : OrthoActorManagerConfiguration<CharacterMetadata>
    {
        public const string DefaultPathPrefix = "Characters";
        public const string DefaultAvatarsPathPrefix = "CharacterAvatars";
        /// <summary>
        /// <see cref="ManagedTextRecord"/> category for the <see cref="CharacterMetadata.DisplayName"/> records.
        /// </summary>
        public const string DisplayNamesCategory = "CharacterNames";

        [Tooltip("Whether to evenly distribute characters by X-axis when adding a new character without a specified position.")]
        public bool AutoArrangeOnAdd = true;
        [Tooltip("Metadata to use by default when creating character actors and custom metadata for the created actor ID doesn't exist.")]
        public CharacterMetadata DefaultMetadata = new CharacterMetadata();
        [Tooltip("Metadata to use when creating character actors with specific IDs.")]
        public CharacterMetadata.Map Metadata = new CharacterMetadata.Map();
        [Tooltip("Configuration of the resource loader used with character avatar texture resources.")]
        public ResourceLoaderConfiguration AvatarLoader = new ResourceLoaderConfiguration { PathPrefix = DefaultAvatarsPathPrefix };

        protected override CharacterMetadata DefaultActorMetadata => DefaultMetadata;
        protected override ActorMetadataMap<CharacterMetadata> ActorMetadataMap => Metadata;

        public CharactersConfiguration ()
        {
            DefaultEasing = EasingType.SmoothStep;
            ZOffset = 50f;
        }
    }
}
