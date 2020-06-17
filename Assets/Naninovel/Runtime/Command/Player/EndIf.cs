// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Closes an [@if] conditional execution block.
    /// For usage examples see [conditional execution](/guide/naninovel-scripts.md#conditional-execution) guide.
    /// </summary>
    public class EndIf : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default) => UniTask.CompletedTask;
    }
}
