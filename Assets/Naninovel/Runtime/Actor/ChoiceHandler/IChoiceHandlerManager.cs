// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IChoiceHandlerActor"/> actors.
    /// </summary>
    public interface IChoiceHandlerManager : IActorManager<IChoiceHandlerActor, ChoiceHandlerState, ChoiceHandlerMetadata, ChoiceHandlersConfiguration>
    {
        
    }
}
