// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Hides (removes) all the actors (eg characters, backgrounds, text printers, choice handlers, etc) on scene.
    /// </summary>
    /// <example>
    /// @hideAll
    /// </example>
    [CommandAlias("hideAll")]
    public class HideAllActors : Command
    {
        /// <summary>
        /// Duration (in seconds) of the fade animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var managers = Engine.GetAllServices<IActorManager>();
            await UniTask.WhenAll(managers.SelectMany(m => m.GetAllActors()).Select(a => a.ChangeVisibilityAsync(false, Duration, cancellationToken: cancellationToken)));
        }
    } 
}
