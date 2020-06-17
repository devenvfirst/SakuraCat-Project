// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides (removes) all the visible characters on scene.
    /// </summary>
    /// <example>
    /// @hideChars
    /// </example>
    [CommandAlias("hideChars")]
    public class HideAllCharacters : Command
    {
        /// <summary>
        /// Duration (in seconds) of the fade animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var manager = Engine.GetService<ICharacterManager>();
            await UniTask.WhenAll(manager.GetAllActors().Select(a => a.ChangeVisibilityAsync(false, Duration, cancellationToken: cancellationToken)));
        }
    }
}
