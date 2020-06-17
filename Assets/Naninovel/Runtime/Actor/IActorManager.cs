// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage <see cref="IActor"/> actors.
    /// </summary>
    public interface IActorManager : IEngineService
    {
        /// <summary>
        /// Configuration of the manager.
        /// </summary>
        ActorManagerConfiguration Configuration { get; }

        /// <summary>
        /// Checks whether an actor with the provided ID is managed by the service. 
        /// </summary>
        bool ActorExists (string actorId);
        /// <summary>
        /// Retrieves a managed actor with the provided ID.
        /// </summary>
        IActor GetActor (string actorId);
        /// <summary>
        /// Retrieves all the actors managed by the service.
        /// </summary>
        IEnumerable<IActor> GetAllActors ();
        /// <summary>
        /// Adds a new managed actor with the provided ID.
        /// </summary>
        UniTask<IActor> AddActorAsync (string actorId);
        /// <summary>
        /// Removes a managed actor with the provided ID.
        /// </summary>
        void RemoveActor (string actorId);
        /// <summary>
        /// Removes all the actors managed by the service.
        /// </summary>
        void RemoveAllActors ();
        /// <summary>
        /// Retrieves state of a managed actor with the provided ID.
        /// </summary>
        ActorState GetActorState (string actorId);
    }

    /// <summary>
    /// Implementation is able to manage <see cref="TActor"/> actors.
    /// </summary>
    /// <typeparam name="TActor">Type of managed actors.</typeparam>
    /// <typeparam name="TState">Type of state describing managed actors.</typeparam>
    /// <typeparam name="TMeta">Type of metadata required to construct managed actors.</typeparam>
    /// <typeparam name="TConfig">Type of the service configuration.</typeparam>
    public interface IActorManager<TActor, TState, TMeta, TConfig> : IActorManager, IEngineService<TConfig>, IStatefulService<GameStateMap>
        where TActor : IActor
        where TState : ActorState<TActor>, new()
        where TMeta : ActorMetadata
        where TConfig : ActorManagerConfiguration<TMeta>
    {
        /// <summary>
        /// Configuration of the manager.
        /// </summary>
        new TConfig Configuration { get; }

        /// <summary>
        /// Adds a new managed actor with the provided ID.
        /// </summary>
        new UniTask<TActor> AddActorAsync (string actorId);
        /// <summary>
        /// Adds a new managed actor with the provided ID and state.
        /// </summary>
        UniTask<TActor> AddActorAsync (string actorId, TState state);
        /// <summary>
        /// Retrieves a managed actor with the provided ID.
        /// </summary>
        new TActor GetActor (string actorId);
        /// <summary>
        /// Retrieves all the actors managed by the service.
        /// </summary>
        new IEnumerable<TActor> GetAllActors ();
        /// <summary>
        /// Retrieves state of a managed actor with the provided ID.
        /// </summary>
        new TState GetActorState (string actorId);
    }
}
