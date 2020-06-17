// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class MoviesConfiguration : Configuration
    {
        public const string DefaultMoviesPathPrefix = "Movies";

        [Tooltip("Configuration of the resource loader used with movie resources.")]
        public ResourceLoaderConfiguration Loader = new ResourceLoaderConfiguration { PathPrefix = DefaultMoviesPathPrefix };
        [Tooltip("Whether to skip movie playback when user activates `cancel` input keys.")]
        public bool SkipOnInput = true;
        [Tooltip("Whether to skip frames to catch up with current time.")]
        public bool SkipFrames = true;
        [Tooltip("Time in seconds to fade in/out before starting/finishing playing the movie.")]
        public float FadeDuration = 1f;
        [Tooltip("Texture to show while fading. Will use a simple black texture when not provided.")]
        public Texture2D CustomFadeTexture = default;
        [Tooltip ("Whether to automatically play a movie after engine initialization and before showing the main menu.")]
        public bool PlayIntroMovie = false;
        [Tooltip("Path to the intro movie resource.")]
        public string IntroMovieName = default;
    }
}
