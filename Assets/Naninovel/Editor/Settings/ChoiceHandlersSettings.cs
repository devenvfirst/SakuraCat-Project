// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEditor;

namespace Naninovel
{
    public class ChoiceHandlersSettings : ActorManagerSettings<ChoiceHandlersConfiguration, IChoiceHandlerActor, ChoiceHandlerMetadata>
    {
        protected override string HelpUri => "guide/choices.html";
        protected override Type ResourcesTypeConstraint => GetTypeConstraint();
        protected override string ResourcesSelectionTooltip => GetTooltip();
        protected override bool AllowMultipleResources => Type.GetType(EditedMetadata?.Implementation)?.FullName != typeof(UIChoiceHandler).FullName;

        private Type GetTypeConstraint ()
        {
            switch (Type.GetType(EditedMetadata?.Implementation)?.Name)
            {
                case nameof(UIChoiceHandler): return typeof(UI.ChoiceHandlerPanel);
                default: return null;
            }
        }

        private string GetTooltip ()
        {
            if (EditedActorId == Configuration.DefaultHandlerId)
                return $"Use `@choice \"Choice summary text.\"` in naninovel scripts to add a choice with this handler.";
            return $"Use `@choice \"Choice summary text.\" handler:{EditedActorId}` in naninovel scripts to add a choice with this handler.";
        }

        [MenuItem("Naninovel/Resources/Choice Handlers")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
