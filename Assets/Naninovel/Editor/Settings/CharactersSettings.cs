// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class CharactersSettings : OrthoActorManagerSettings<CharactersConfiguration, ICharacterActor, CharacterMetadata>
    {
        private static readonly GUIContent AvatarsEditorContent = new GUIContent("Avatar Resources",
            "Use 'CharacterId/Appearance' name to map avatar texture to a character appearance. Use 'CharacterId/Default' to map a default avatar to the character.");

        protected override string HelpUri => "guide/characters.html";
        protected override Type ResourcesTypeConstraint => GetTypeConstraint();
        protected override string ResourcesSelectionTooltip => GetTooltip();
        protected override bool AllowMultipleResources =>
            #if SPRITE_DICING_AVAILABLE
            Type.GetType(EditedMetadata?.Implementation)?.FullName != typeof(DicedSpriteCharacter).FullName &&
            #endif
            Type.GetType(EditedMetadata?.Implementation)?.FullName != typeof(GenericCharacter).FullName &&
            Type.GetType(EditedMetadata?.Implementation)?.FullName != typeof(LayeredCharacter).FullName;
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers
        {
            get
            {
                var overrideConfigurationDrawers = base.OverrideConfigurationDrawers;
                overrideConfigurationDrawers[nameof(CharactersConfiguration.AvatarLoader)] = DrawAvatarsEditor;
                return overrideConfigurationDrawers;
            }
        }
        protected override Dictionary<string, Action<SerializedProperty>> OverrideMetaDrawers
        {
            get
            {
                var drawers = base.OverrideMetaDrawers;
                drawers[nameof(CharacterMetadata.CustomShader)] = property => { if (GetTypeConstraint() != typeof(CharacterActorBehaviour)) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.NameColor)] = property => { if (EditedMetadata.UseCharacterColor) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.MessageColor)] = property => { if (EditedMetadata.UseCharacterColor) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.HighlightCharacterCount)] = property => { if (EditedMetadata.HighlightWhenSpeaking) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.SpeakingTint)] = property => { if (EditedMetadata.HighlightWhenSpeaking) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.NotSpeakingTint)] = property => { if (EditedMetadata.HighlightWhenSpeaking) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.PlaceOnTop)] = property => { if (EditedMetadata.HighlightWhenSpeaking) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.HighlightDuration)] = property => { if (EditedMetadata.HighlightWhenSpeaking) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.HighlightEasing)] = property => { if (EditedMetadata.HighlightWhenSpeaking) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.MessageSound)] = property => EditorResources.DrawPathPopup(property, AudioConfiguration.DefaultAudioPathPrefix, AudioConfiguration.DefaultAudioPathPrefix, "None (disabled)");
                drawers[nameof(CharacterMetadata.ClipMessageSound)] = property => { if (!string.IsNullOrEmpty(EditedMetadata.MessageSound)) EditorGUILayout.PropertyField(property); };
                drawers[nameof(CharacterMetadata.LinkedPrinter)] = property => EditorResources.DrawPathPopup(property, $"{TextPrintersConfiguration.DefaultPathPrefix}/*", "*", "None (disabled)");
                return drawers;
            }
        }

        private bool avatarsEditorExpanded;

        private Type GetTypeConstraint ()
        {
            switch (Type.GetType(EditedMetadata?.Implementation)?.Name)
            {
                case nameof(SpriteCharacter): return typeof(UnityEngine.Texture2D);
                case nameof(GenericCharacter): return typeof(CharacterActorBehaviour);
                case nameof(LayeredCharacter): return typeof(LayeredActorBehaviour);
                #if SPRITE_DICING_AVAILABLE
                case nameof(DicedSpriteCharacter): return typeof(SpriteDicing.DicedSpriteAtlas);
                #endif
                default: return null;
            }
        }

        private string GetTooltip ()
        {
            if (AllowMultipleResources)
                return $"Use `@char {EditedActorId}.%name%` in naninovel scripts to show the character with selected appearance.";
            return $"Use `@char {EditedActorId}` in naninovel scripts to show this character.";
        }

        private void DrawAvatarsEditor (SerializedProperty avatarsLoaderProperty)
        {
            EditorGUILayout.PropertyField(avatarsLoaderProperty);

            avatarsEditorExpanded = EditorGUILayout.Foldout(avatarsEditorExpanded, AvatarsEditorContent, true);
            if (!avatarsEditorExpanded) return;
            ResourcesEditor.DrawGUILayout(Configuration.AvatarLoader.PathPrefix, AllowRename, Configuration.AvatarLoader.PathPrefix, null, typeof(Texture2D),
                "Use `@char CharacterID avatar:%name%` in naninovel scripts to assign selected avatar texture for the character.");
        }

        [MenuItem("Naninovel/Resources/Characters")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
