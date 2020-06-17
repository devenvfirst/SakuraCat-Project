// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Removes all the messages from [printer backlog](/guide/printer-backlog.md).
    /// </summary>
    /// <example>
    /// @clearBacklog
    /// </example>
    public class ClearBacklog : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            Engine.GetService<IUIManager>()?.GetUI<UI.IBacklogUI>()?.Clear();
            return UniTask.CompletedTask;
        }
    }
}
