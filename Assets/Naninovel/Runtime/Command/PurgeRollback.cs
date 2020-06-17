// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Prevents player from rolling back to the previous state snapshots.
    /// </summary>
    /// <example>
    /// ; Prevent player from rolling back to try picking another choice.
    /// 
    /// @choice "One" goto:.One
    /// @choice "Two" goto:.Two
    /// @stop
    /// 
    /// # One
    /// @purgeRollback
    /// You've picked one.
    /// 
    /// # Two
    /// @purgeRollback
    /// You've picked two.
    /// </example>
    public class PurgeRollback : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            Engine.GetService<IStateManager>()?.PurgeRollbackData();
            return UniTask.CompletedTask;
        }
    }
}
