// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class TextPrintersSettings : OrthoActorManagerSettings<TextPrintersConfiguration, ITextPrinterActor, TextPrinterMetadata>
    {
        protected override string HelpUri => "guide/text-printers.html";
        protected override Type ResourcesTypeConstraint => GetTypeConstraint();
        protected override string ResourcesSelectionTooltip => GetTooltip();
        protected override bool AllowMultipleResources => Type.GetType(EditedMetadata?.Implementation)?.FullName != typeof(UITextPrinter).FullName;
        protected override Dictionary<string, Action<SerializedProperty>> OverrideMetaDrawers
        {
            get
            {
                var drawers = base.OverrideMetaDrawers;
                drawers[nameof(TextPrinterMetadata.EnableDepthPass)] = null;
                drawers[nameof(TextPrinterMetadata.DepthAlphaCutoff)] = null;
                drawers[nameof(TextPrinterMetadata.CustomShader)] = null;
                return drawers;
            }
        }

        private Type GetTypeConstraint ()
        {
            switch (Type.GetType(EditedMetadata?.Implementation)?.Name)
            {
                case nameof(UITextPrinter): return typeof(UI.UITextPrinterPanel);
                default: return null;
            }
        }

        private string GetTooltip ()
        {
            if (EditedActorId == Configuration.DefaultPrinterId)
                return "This printer will be active by default: all the generic text and `@print` commands will use it to output the text. Use `@printer PrinterID` action to change active printer.";
            return $"Use `@printer {EditedActorId}` in naninovel scripts to set this printer active; all the consequent generic text and `@print` commands will then use it to output the text.";
        }

        [MenuItem("Naninovel/Resources/Text Printers")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
