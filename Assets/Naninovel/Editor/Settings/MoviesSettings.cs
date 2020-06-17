// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using UnityEditor;

namespace Naninovel
{
    public class MoviesSettings : ResourcefulSettings<MoviesConfiguration>
    {
        protected override string HelpUri => "guide/movies.html";

        protected override Type ResourcesTypeConstraint => typeof(UnityEngine.Video.VideoClip);
        protected override string ResourcesCategoryId => Configuration.Loader.PathPrefix;
        protected override string ResourcesSelectionTooltip => "Use `@movie %name%` in naninovel scripts to play a movie of the selected video clip.";
        protected override Dictionary<string, Action<SerializedProperty>> OverrideConfigurationDrawers => new Dictionary<string, Action<SerializedProperty>> {
            [nameof(MoviesConfiguration.IntroMovieName)] = property => { if (Configuration.PlayIntroMovie) EditorResources.DrawPathPopup(property, ResourcesCategoryId, ResourcesPathPrefix); }
        };

        [MenuItem("Naninovel/Resources/Movies")]
        private static void OpenResourcesWindow () => OpenResourcesWindowImpl();
    }
}
