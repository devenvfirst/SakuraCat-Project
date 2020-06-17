// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Automatically save the game to a quick save slot.
    /// </summary>
    /// <example>
    /// @save
    /// </example>
    [CommandAlias("save")]
    public class AutoSave : Command, Command.IForceWait
    {
        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            await Engine.GetService<IStateManager>().QuickSaveAsync();
        }
    } 
}
