// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Resets state of the [engine services](https://naninovel.com/guide/engine-services.html) and unloads (disposes) 
    /// all the resources loaded by Naninovel (textures, audio, video, etc); will basically revert to an empty initial engine state.
    /// </summary>
    /// <remarks>
    /// The process is asynchronous and is masked with a loading screen ([ILoadingUI](https://naninovel.com/guide/user-interface.html#ui-customization)).
    /// <br/><br/>
    /// When [ResetStateOnLoad](https://naninovel.com/guide/configuration.html#state) is disabled in the configuration, you can use this command
    /// to manually dispose unused resources to prevent memory leak issues.
    /// <br/><br/>
    /// Be aware, that this command can not be undone (rewinded back).
    /// </remarks>
    /// <example>
    /// ; Reset all the services.
    /// @resetState
    /// 
    /// ; Reset all the services except variable and audio managers (current audio will continue playing).
    /// @resetState ICustomVariableManager,IAudioManager
    /// </example>
    public class ResetState : Command, Command.IForceWait
    {
        /// <summary>
        /// Name of the [engine services](https://naninovel.com/guide/engine-services.html) (interfaces) to exclude from reset.
        /// When specifying the parameter, consider always adding `ICustomVariableManager` to preserve the local variables.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringListParameter Exclude = new List<string> { "ICustomVariableManager" };

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            await Engine.GetService<IStateManager>().ResetStateAsync(Exclude);
        }
    }
}
