// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Playes a movie with the provided name (path).
    /// </summary>
    /// <remarks>
    /// Will fade-out the screen before playing the movie and fade back in after the play.
    /// Playback can be canceled by activating a `cancel` input (`Esc` key by default).
    /// </remarks>
    /// <example>
    /// ; Given an "Opening" video clip is added to the movie resources, plays it
    /// @movie Opening
    /// </example>
    [CommandAlias("movie")]
    public class PlayMovie : Command, Command.IPreloadable
    {
        /// <summary>
        /// Name of the movie resource to play.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter MovieName;

        protected IMoviePlayer Player => Engine.GetService<IMoviePlayer>();

        public async UniTask HoldResourcesAsync ()
        {
            if (!Assigned(MovieName) || MovieName.DynamicValue) return;
            await Player?.HoldResourcesAsync(this, MovieName);
        }

        public void ReleaseResources ()
        {
            if (!Assigned(MovieName) || MovieName.DynamicValue) return;
            Player?.ReleaseResources(this, MovieName);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            await Player?.PlayAsync(MovieName, cancellationToken);
        }
    }
}
