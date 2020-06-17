// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <inheritdoc cref="IChoiceHandlerManager"/>
    [InitializeAtRuntime]
    public class ChoiceHandlerManager : ActorManager<IChoiceHandlerActor, ChoiceHandlerState, ChoiceHandlerMetadata, ChoiceHandlersConfiguration>, IChoiceHandlerManager
    {
        public ChoiceHandlerManager (ChoiceHandlersConfiguration config)
            : base(config) { }
    }
}
