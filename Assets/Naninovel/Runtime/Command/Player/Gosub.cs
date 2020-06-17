// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Navigates naninovel script playback to the provided path and saves that path to global state; 
    /// [@return] commands use this info to redirect to command after the last invoked gosub command. 
    /// Designed to serve as a function (subroutine) in a programming language, allowing to reuse a piece of naninovel script.
    /// Useful for invoking a repeating set of commands multiple times.
    /// </summary>
    /// <remarks>
    /// It's possible to declare a gosub outside of the currently played script and use it from any other scripts;
    /// by default state reset won't happen when you're loading another script to play a gosub or returning back to 
    /// prevent delays and loading screens. Be aware though, that all the resources referenced in gosub script will be
    /// held until the next state reset.
    /// </remarks>
    /// <example>
    /// ; Navigate the playback to the label `VictoryScene` in the currently played script,
    /// ; executes the commands and navigates back to the command after the `gosub`.
    /// @gosub .VictoryScene
    /// ...
    /// @stop
    /// 
    /// # VictoryScene
    /// @back Victory
    /// @sfx Fireworks
    /// @bgm Fanfares
    /// You are victorious!
    /// @return
    /// 
    /// ; Another example with some branching inside the subroutine.
    /// @set time=10
    /// ; Here we get one result
    /// @gosub .Room
    /// ...
    /// @set time=3
    /// ; And here we get another
    /// @gosub .Room
    /// ...
    /// 
    /// # Room
    /// @print "It's too early, I should visit this place when it's dark." if:time&lt;21&amp;&amp;time>6
    /// @print "I can sense an ominous presence here!" if:time>21&amp;&amp;time&lt;6
    /// @return
    /// </example>
    public class Gosub : Command, Command.IForceWait
    {
        /// <summary>
        /// Path to navigate into in the following format: `ScriptName.LabelName`.
        /// When label name is ommited, will play provided script from the start.
        /// When script name is ommited, will attempt to find a label in the currently played script.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public NamedStringParameter Path;
        /// <summary>
        /// When specified, will reset the engine services state before loading a script (in case the path is leading to another script).
        /// Specify `*` to reset all the services (except variable manager), or specify service names to exclude from reset.
        /// By default, the state does not reset.
        /// </summary>
        [ParameterAlias("reset")]
        public StringListParameter ResetState;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var player = Engine.GetService<IScriptPlayer>();

            var spot = new PlaybackSpot(player.PlayedScript.Name, player.PlayedCommand?.PlaybackSpot.LineIndex + 1 ?? 0, 0);
            player.GosubReturnSpots.Push(spot);

            var resetState = Assigned(ResetState) ? ResetState : (StringListParameter)new List<string> { Goto.NoResetFlag };
            await new Goto { Path = Path, ResetState = resetState }.ExecuteAsync(cancellationToken);
        }
    } 
}
