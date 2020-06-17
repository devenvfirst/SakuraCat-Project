// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Allows to enable or disable script player "skip" mode.
    /// </summary>
    /// <example>
    /// ; Enable skip mode
    /// @skip
    /// ; Disable skip mode
    /// @skip false
    /// </example>
    public class Skip : Command
    {
        /// <summary>
        /// Whether to enable (default) or disable the skip mode.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public BooleanParameter Enable = true;

        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            scriptPlayer.SetSkipEnabled(Enable);
            return UniTask.CompletedTask;
        }
    }
}
