// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops the naninovel script execution.
    /// </summary>
    /// <example>
    /// Show the choices and halt script execution until the player picks one.
    /// @choice "Choice 1"
    /// @choice "Choice 2"
    /// @stop
    /// We'll get here after player will make a choice.
    /// </example>
    public class Stop : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            Engine.GetService<IScriptPlayer>().Stop();

            return UniTask.CompletedTask;
        }
    } 
}
