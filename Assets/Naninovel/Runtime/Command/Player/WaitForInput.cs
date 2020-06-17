// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Holds script execution until user activates a `continue` input.
    /// Shortcut for `@wait i`.
    /// </summary>
    /// <example>
    /// ; User will have to activate a `continue` input after the first sentence 
    /// ; for the printer to contiue printing out the following text.
    /// Lorem ipsum dolor sit amet.[i] Consectetur adipiscing elit.
    /// </example>
    [CommandAlias("i")]
    public class WaitForInput : Command, Command.IForceWait
    {
        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var waitAction = new Wait();
            waitAction.WaitMode = Commands.Wait.InputLiteral;
            await waitAction.ExecuteAsync(cancellationToken);
        }
    }
}
