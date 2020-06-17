// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents data required to construct and initialize a <see cref="ITextPrinterActor"/>.
    /// </summary>
    [System.Serializable]
    public class TextPrinterMetadata : OrthoActorMetadata
    {
        [System.Serializable]
        public class Map : ActorMetadataMap<TextPrinterMetadata> { }

        [Tooltip("Whether to automatically reset the printer on each `@print` command (unless `reset` parameter is explicitly disabled).")]
        public bool AutoReset = true;
        [Tooltip("Whether to automatically make the printer default and hide other printers on each `@print` command (unless `default` parameter is explicitly disabled).")]
        public bool AutoDefault = true;
        [Tooltip("Whether to automatically wait for user input on each `@print` command (unless `waitInput` parameter is explicitly disabled).")]
        public bool AutoWait = true;
        [Tooltip("Number of line breaks to automatically insert before the printed text on each `@print` command when the printer already contains some text (unless `br` parameter is explicitly specified).")]
        public int AutoLineBreak = 0;
        [Tooltip("Default visibility change animation duration; used when corresponding parameter is not specified in script command.")]
        public float ChangeVisibilityDuration = .3f;

        public TextPrinterMetadata ()
        {
            Implementation = typeof(UITextPrinter).AssemblyQualifiedName;
            Loader = new ResourceLoaderConfiguration { PathPrefix = TextPrintersConfiguration.DefaultPathPrefix };
            Pivot = new Vector2(.5f, .5f);
        }

        public override TState GetPoseOrNull<TState> (string poseName) => null;
    }
}
