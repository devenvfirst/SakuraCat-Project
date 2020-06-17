// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="IActorManager"/>.
    /// </summary>
    public static class ActorManagerExtensions
    {
        /// <summary>
        /// Returns a managed actor with the provided ID. If the actor doesn't exist, will add it.
        /// </summary>
        public static async UniTask<IActor> GetOrAddActorAsync (this IActorManager mngr, string actorId)
        {
            return mngr.ActorExists(actorId) ? mngr.GetActor(actorId) : await mngr.AddActorAsync(actorId);
        }

        /// <summary>
        /// Returns a managed actor with the provided ID. If the actor doesn't exist, will add it.
        /// </summary>
        public static async UniTask<TActor> GetOrAddActorAsync<TActor, TState, TMeta, TConfig> (this IActorManager<TActor, TState, TMeta, TConfig> mngr, string actorId)
            where TActor : IActor
            where TState : ActorState<TActor>, new()
            where TMeta : ActorMetadata
            where TConfig : ActorManagerConfiguration<TMeta>
        {
            return mngr.ActorExists(actorId) ? mngr.GetActor(actorId) : await mngr.AddActorAsync(actorId);
        }
    }
}
