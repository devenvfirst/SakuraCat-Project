// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to handle movie playing.
    /// </summary>
    public interface IMoviePlayer : IEngineService<MoviesConfiguration>
    {
        /// <summary>
        /// Event invoked when playback is started.
        /// </summary>
        event Action OnMoviePlay;
        /// <summary>
        /// Event invoked when playback is stopped.
        /// </summary>
        event Action OnMovieStop;
        /// <summary>
        /// Event invoked when a movie is ready to be played.
        /// </summary>
        event Action<Texture> OnMovieTextureReady;

        /// <summary>
        /// Whether currently playing a movie.
        /// </summary>
        bool Playing { get; }
        /// <summary>
        /// A fade texture used when starting and stopping the playback.
        /// </summary>
        Texture2D FadeTexture { get; }

        /// <summary>
        /// Plays a movie with the provided name; returns when the playback finishes.
        /// </summary>
        UniTask PlayAsync (string movieName, CancellationToken cancellationToken = default);
        /// <summary>
        /// Stops the playback.
        /// </summary>
        void Stop ();
        /// <summary>
        /// Preloads the resources required to play a movie with the provided path.
        /// </summary>
        UniTask HoldResourcesAsync (object holder, string movieName);
        /// <summary>
        /// Unloads the resources required to play a movie with the provided path.
        /// </summary>
        void ReleaseResources (object holder, string movieName);
    }
}
