// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Shows (makes visible) actors (character, background, text printer, choice handler, etc) with the specified IDs.
    /// In case mutliple actors with the same ID found (eg, a character and a printer), will affect only the first found one.
    /// </summary>
    /// <example>
    /// ; Given an actor with ID `SomeActor` is hidden, reveal (fade-in) it over 3 seconds.
    /// @show SomeActor time:3
    /// 
    /// ; Show `Kohaku` and `Yuko` actors.
    /// @show Kohaku,Yuko
    /// </example>
    [CommandAlias("show")]
    public class ShowActors : Command
    {
        /// <summary>
        /// IDs of the actors to show.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringListParameter ActorIds;
        /// <summary>
        /// Duration (in seconds) of the fade animation. Default value: 0.35 seconds.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration = .35f;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var managers = Engine.GetAllServices<IActorManager>(c => ActorIds.Any(id => c.ActorExists(id)));

            var tasks = new List<UniTask>();
            foreach (var actorId in ActorIds)
                if (managers.FirstOrDefault(m => m.ActorExists(actorId)) is IActorManager manager)
                    tasks.Add(manager.GetActor(actorId).ChangeVisibilityAsync(true, Duration, cancellationToken: cancellationToken));
                else LogErrorWithPosition($"Failed to show `{actorId}` actor: can't find any managers with `{actorId}` actor.");

            await UniTask.WhenAll(tasks);
        }
    } 
}
