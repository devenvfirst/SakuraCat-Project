// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    /// <inheritdoc cref="IMovieUI"/>
    public class MovieUI : CustomUI, IMovieUI
    {
        protected RawImage MovieImage => movieImage;
        protected RawImage FadeImage => fadeImage;

        [SerializeField] private RawImage movieImage = default;
        [SerializeField] private RawImage fadeImage = default;

        private IMoviePlayer moviePlayer;

        protected override void Awake ()
        {
            base.Awake();

            this.AssertRequiredObjects(movieImage, fadeImage);
            moviePlayer = Engine.GetService<IMoviePlayer>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            moviePlayer.OnMoviePlay += HandleMoviePlay;
            moviePlayer.OnMovieStop += HandleMovieStop;
            moviePlayer.OnMovieTextureReady += HandleMovieTextureReady;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            moviePlayer.OnMoviePlay -= HandleMoviePlay;
            moviePlayer.OnMovieStop -= HandleMovieStop;
            moviePlayer.OnMovieTextureReady -= HandleMovieTextureReady;
        }

        protected virtual async void HandleMoviePlay ()
        {
            fadeImage.texture = moviePlayer.FadeTexture;
            movieImage.SetOpacity(0);
            await ChangeVisibilityAsync(true, moviePlayer.Configuration.FadeDuration);
            movieImage.SetOpacity(1);
        }

        protected virtual void HandleMovieTextureReady (Texture texture)
        {
            movieImage.texture = texture;
        }

        protected virtual async void HandleMovieStop ()
        {
            movieImage.SetOpacity(0);
            await ChangeVisibilityAsync(false, moviePlayer.Configuration.FadeDuration);
        }
    }
}
