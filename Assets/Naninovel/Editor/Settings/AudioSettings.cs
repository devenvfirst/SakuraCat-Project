// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    public class AudioSettings : ResourcefulSettings<AudioConfiguration>
    {
        protected override string HelpUri => "guide/audio.html#background-music";

        protected override Type ResourcesTypeConstraint => typeof(AudioClip);
        protected override string ResourcesCategoryId => Configuration.AudioLoader.PathPrefix;
        protected override string ResourcesSelectionTooltip => "Use `@bgm %name%` or `@sfx %name%` in naninovel scripts to play a background music or sound effect of the selected audio clip.";

        [MenuItem("Naninovel/Resources/Audio")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
