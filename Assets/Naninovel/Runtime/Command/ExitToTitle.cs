// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Resets engine state and shows `ITitleUI` UI (main menu).
    /// </summary>
    /// <example>
    /// @title
    /// </example>
    [CommandAlias("title")]
    public class ExitToTitle : Command, Command.IForceWait
    {
        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var gameState = Engine.GetService<IStateManager>();
            var uiManager = Engine.GetService<IUIManager>();

            await gameState.ResetStateAsync();
            // Don't check for the cancellation token, as it's always cancelled after state reset.

            uiManager.GetUI<UI.ITitleUI>()?.Show();
        }
    }
}
