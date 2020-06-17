// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class BackgroundsSettings : OrthoActorManagerSettings<BackgroundsConfiguration, IBackgroundActor, BackgroundMetadata>
    {
        protected override string HelpUri => "guide/backgrounds.html";
        protected override Type ResourcesTypeConstraint => GetTypeConstraint();
        protected override string ResourcesSelectionTooltip => GetTooltip();
        protected override bool AllowMultipleResources =>
            Type.GetType(EditedMetadata?.Implementation)?.FullName != typeof(GenericBackground).FullName &&
            Type.GetType(EditedMetadata?.Implementation)?.FullName != typeof(LayeredBackground).FullName;
        protected override HashSet<string> LockedActorIds => new HashSet<string> { BackgroundsConfiguration.MainActorId };

        private static bool editMainRequested;

        public override void OnGUI (string searchContext)
        {
            if (editMainRequested)
            {
                editMainRequested = false;
                MetadataMapEditor.SelectEditedMetadata(BackgroundsConfiguration.MainActorId);
            }

            base.OnGUI(searchContext);
        }

        private Type GetTypeConstraint ()
        {
            switch (Type.GetType(EditedMetadata?.Implementation)?.Name)
            {
                case nameof(SpriteBackground): return typeof(UnityEngine.Texture2D);
                case nameof(GenericBackground): return typeof(BackgroundActorBehaviour);
                case nameof(SceneBackground): return typeof(SceneAsset);
                case nameof(VideoBackground): return typeof(UnityEngine.Video.VideoClip);
                case nameof(LayeredBackground): return typeof(LayeredActorBehaviour);
                default: return null;
            }
        }

        private string GetTooltip ()
        {
            if (EditedActorId == BackgroundsConfiguration.MainActorId && AllowMultipleResources)
                return "Use `@back %name%` in naninovel scripts to show main background with the selected appearance.";
            else if (AllowMultipleResources)
                return $"Use `@back %name% id:{EditedActorId}` in naninovel scripts to show this background with the selected appearance.";
            return $"Use `@back id:{EditedActorId}` in naninovel scripts to show this background.";
        }

        [MenuItem("Naninovel/Resources/Backgrounds")]
        private static void OpenResourcesWindow ()
        {
            // Automatically open main background editor when opened via resources context menu.
            editMainRequested = true;
            OpenResourcesWindowImpl();
        }
    }
}
