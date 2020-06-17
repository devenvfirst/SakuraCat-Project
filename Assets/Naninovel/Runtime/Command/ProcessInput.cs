// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Allows halting and resuming user input processing (eg, reacting to pressing keyboard keys).
    /// The effect of the action is persistent and saved with the game.
    /// </summary>
    /// <example>
    /// ; Halt input processing
    /// @processInput false
    /// ; Resume input processing
    /// @processInput true
    /// </example>
    public class ProcessInput : Command
    {
        /// <summary>
        /// Whether to enable input processing.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public BooleanParameter InputEnabled = true;

        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var inputManager = Engine.GetService<IInputManager>();
            inputManager.ProcessInput = InputEnabled;

            return UniTask.CompletedTask;
        }
    }
}
