// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Destroys an object spawned with [@spawn] command.
    /// </summary>
    /// <remarks>
    /// If prefab has a <see cref="MonoBehaviour"/> component attached the root object, and the component implements
    /// a <see cref="IParameterized"/> interface, will pass the specified `params` values before destroying the object;
    /// if the component implements <see cref="IAwaitable"/> interface, command execution will wait for
    /// the async completion task returned by the implementation before destroying the object.
    /// </remarks>
    /// <example>
    /// ; Given a "@spawn Rainbow" command was executed before
    /// @despawn Rainbow
    /// </example>
    [CommandAlias("despawn")]
    public class DestroySpawned : Command
    {
        public interface IParameterized { void SetDestroyParameters (string[] parameters); }
        public interface IAwaitable { UniTask AwaitDestroyAsync (CancellationToken cancellationToken = default); }

        /// <summary>
        /// Name (path) of the prefab resource to destroy.
        /// A [@spawn] command with the same parameter is expected to be executed before.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter Path;
        /// <summary>
        /// Parameters to set before destoying the prefab.
        /// Requires the prefab to have a <see cref="IParameterized"/> component attached the root object.
        /// </summary>
        public StringListParameter Params;

        protected virtual ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (!SpawnManager.IsObjectSpawned(Path))
            {
                Debug.LogWarning($"Failed to destroy spawned object '{Path}': the object is not found.");
                return;
            }

            await SpawnManager.DestroySpawnedAsync(Path, cancellationToken, Params);
        }
    }
}
