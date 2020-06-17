// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Instantiates a prefab or a [special effect](/guide/special-effects.md);
    /// when performed over an already spawned object, will update the spawn parameters instead.
    /// </summary>
    /// <remarks>
    /// If prefab has a <see cref="MonoBehaviour"/> component attached the root object, and the component implements
    /// a <see cref="IParameterized"/> interface, will pass the specified `params` values after the spawn;
    /// if the component implements <see cref="IAwaitable"/> interface, command execution will wait for
    /// the async completion task returned by the implementation.
    /// </remarks>
    /// <example>
    /// ; Given an `Rainbow` prefab is assigned in spawn resources, instantiate it.
    /// @spawn Rainbow
    /// </example>
    public class Spawn : Command, Command.IPreloadable
    {
        public interface IParameterized { void SetSpawnParameters (string[] parameters); }
        public interface IAwaitable { UniTask AwaitSpawnAsync (CancellationToken cancellationToken = default); }

        /// <summary>
        /// Name (path) of the prefab resource to spawn.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter Path;
        /// <summary>
        /// Parameters to set when spawning the prefab.
        /// Requires the prefab to have a <see cref="IParameterized"/> component attached the root object.
        /// </summary>
        public StringListParameter Params;

        protected virtual ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();

        public async UniTask HoldResourcesAsync ()
        {
            if (!Assigned(Path) || Path.DynamicValue || string.IsNullOrWhiteSpace(Path)) return;
            await SpawnManager.HoldResourcesAsync(this, Path);
        }

        public void ReleaseResources ()
        {
            if (!Assigned(Path) || Path.DynamicValue || string.IsNullOrWhiteSpace(Path)) return;
            SpawnManager.ReleaseResources(this, Path);
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            if (SpawnManager.IsObjectSpawned(Path)) // Update params if already spawned.
                await SpawnManager.UpdateSpawnedAsync(Path, cancellationToken, Params);
            else await SpawnManager.SpawnAsync(Path, cancellationToken, Params);
        }
    }
}
