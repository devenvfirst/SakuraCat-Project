// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Can be used in generic text lines to prevent activating `wait for input` mode when the text is printed.
    /// </summary>
    /// <example>
    /// ; Script player won't wait for `continue` input before executing the `@sfx` command.
    /// And the rain starts.[skipInput]
    /// @sfx Rain
    /// </example>
    public class SkipInput : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default) => UniTask.CompletedTask;
    } 
}
