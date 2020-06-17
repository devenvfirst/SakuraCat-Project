// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// Implementation represents a <see cref="IEngineService"/> that has a persistent 
    /// state and is able to save/load it using <typeparamref name="TState"/>.
    /// </summary>
    public interface IStatefulService<TState> : IEngineService where TState : StateMap
    {
        /// <summary>
        /// Serializes service state via <see cref="TState.SetState{TState}(TState, string)"/>.
        /// </summary>
        void SaveServiceState (TState state);
        /// <summary>
        /// De-serializes service state via <see cref="TState.GetState{TState}(string)"/>.
        /// </summary>
        UniTask LoadServiceStateAsync (TState state);
    }
}
