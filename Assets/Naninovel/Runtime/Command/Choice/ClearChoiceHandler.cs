// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Removes all the choice options in the choice handler with the provided ID (or in default one, when ID is not specified; 
    /// or in all the existing handlers, when `*` is specified as ID) and (optionally) hides it (them).
    /// </summary>
    /// <example>
    /// ; Add choices and remove them after a set time (in case the player didn't pick one).
    /// # Start
    /// You have 2 seconds to respond![skipInput]
    /// @choice "Cats" goto:.PickedChoice 
    /// @choice "Dogs" goto:.PickedChoice
    /// @wait 2
    /// @clearChoice
    /// Too late!
    /// @goto .Start
    /// # PickedChoice
    /// Good!
    /// </example>
    [CommandAlias("clearChoice")]
    public class ClearChoiceHandler : Command
    {
        /// <summary>
        /// ID of the choice handler to clear. Will use a default handler if not provided.
        /// Specify `*` to clear all the existing handlers.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringParameter HandlerId;
        /// <summary>
        /// Whether to also hide the affected choice handlers.
        /// </summary>
        public BooleanParameter Hide = true;

        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var mngr = Engine.GetService<IChoiceHandlerManager>();

            if (Assigned(HandlerId) && HandlerId == "*")
            {
                foreach (var handler in mngr.GetAllActors())
                {
                    RemoveAllChoices(handler);
                    if (Hide) handler.Visible = false;
                }
                return UniTask.CompletedTask;
            }

            var handlerId = Assigned(HandlerId) ? HandlerId.Value : mngr.Configuration.DefaultHandlerId;
            if (!mngr.ActorExists(handlerId))
            {
                LogWarningWithPosition($"Failed to clear `{handlerId}` choice handler: handler actor with the provided ID doesn't exist.");
                return UniTask.CompletedTask;
            }

            var choiceHandler = mngr.GetActor(handlerId);
            RemoveAllChoices(choiceHandler);
            if (Hide) choiceHandler.Visible = false;
            return UniTask.CompletedTask;
        }

        private static void RemoveAllChoices (IChoiceHandlerActor choiceHandler)
        {
            foreach (var choiceState in choiceHandler.Choices.ToList())
                choiceHandler.RemoveChoice(choiceState.Id);
        }
    }
}
