// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents data required to construct and initialize a <see cref="ICharacterActor"/>.
    /// </summary>
    [System.Serializable]
    public class CharacterMetadata : OrthoActorMetadata
    {
        [System.Serializable]
        public class Map : ActorMetadataMap<CharacterMetadata> { }
        [System.Serializable]
        public class Pose : ActorPose<CharacterState> { }

        [Tooltip("Look direction as portrait (baked) on the character texture; required to properly flip characters to make them 'face' the right side of the screen.")]
        public CharacterLookDirection BakedLookDirection = CharacterLookDirection.Left;
        [Tooltip("Full name of the character to display in printer name label UI. Will use character ID when not specified.\nIt's possible to localize the display names or bind them to a custom variable (and dynamically change throughout the game); see the guide on `Characters` -> `Display Names` for more info.")]
        public string DisplayName = default;
        [Tooltip("Whether to apply character-specific color to printer messages and name label UI.")]
        public bool UseCharacterColor = false;
        [Tooltip("Character-specific color to tint printer name label UI.")]
        public Color NameColor = Color.white;
        [Tooltip("Character-specific color to tint printer messages.")]
        public Color MessageColor = Color.white;
        [Tooltip("When enabled, will apply specified tint colors based on whether this actor is the author of the last printed text.")]
        public bool HighlightWhenSpeaking = false;
        [Tooltip("Highlight will happen only when the specified number of characters are visible on scene.")]
        public int HighlightCharacterCount = 0;
        [Tooltip("Tint color to apply when the character is speaking.")]
        public Color SpeakingTint = Color.white;
        [Tooltip("Tint color to apply when the character is not speaking.")]
        public Color NotSpeakingTint = Color.gray;
        [Tooltip("Whether to also move the highlighted character to the topmost position (closer to the camera over z-axis).")]
        public bool PlaceOnTop = true;
        [Tooltip("The highlight tint animation duration.")]
        public float HighlightDuration = .35f;
        [Tooltip("The highlight tint animation easing.")]
        public EasingType HighlightEasing = EasingType.SmoothStep;
        [Tooltip("Path to the sound (SFX) to play when printing (revealing) messages and the character is author. The sound will be played on each character reveal, so make sure it's very short and sharp (without any pause/silence at the beginning of the audio clip).")]
        public string MessageSound = default;
        [Tooltip("Controls whether to clip (restart if playing) the message sound on consequent character reveals.")]
        public bool ClipMessageSound = true;
        [Tooltip("When the character is an author of a printed message, selected text printer will automatically be used to handle the printing. Only custom printers are allowed.")]
        public string LinkedPrinter = default;
        [Tooltip("Named states (poses) of the character; pose name can be used as appearance in `@char` commands to apply associated state.")]
        public List<Pose> Poses = new List<Pose>();

        public CharacterMetadata ()
        {
            Implementation = typeof(SpriteCharacter).AssemblyQualifiedName;
            Loader = new ResourceLoaderConfiguration { PathPrefix = CharactersConfiguration.DefaultPathPrefix };
            Pivot = new Vector2(.5f, .0f);
        }

        public override TState GetPoseOrNull<TState> (string poseName) => Poses.FirstOrDefault(p => p.Name == poseName)?.ActorState as TState;
    }
}
