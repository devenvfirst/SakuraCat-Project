// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage objects spawned with <see cref="Commands.Spawn"/> commands.
    /// </summary>
    public interface ISpawnManager : IEngineService<SpawnConfiguration>
    {
        /// <summary>
        /// Attempts to spawn a <see cref="GameObject"/> based on the prefab stored at the provided path.
        /// </summary>
        UniTask SpawnAsync (string path, CancellationToken cancellationToken = default, params string[] parameters);
        /// <summary>
        /// Attempts to update parameters of a spawned <see cref="GameObject"/>; can only be used over <see cref="Commands.Spawn.IParameterized"/> objects.
        /// </summary>
        UniTask UpdateSpawnedAsync (string path, CancellationToken cancellationToken = default, params string[] parameters);
        /// <summary>
        /// Attempts to destroy a previously spawned <see cref="GameObject"/> with the provided path while applying provided parameters.
        /// </summary>
        /// <returns>Whether the object was found and destroyed.</returns>
        UniTask<bool> DestroySpawnedAsync (string path, CancellationToken cancellationToken = default, params string[] parameters);
        /// <summary>
        /// Attempts to destroy a previously spawned <see cref="GameObject"/> with the provided path.
        /// </summary>
        /// <returns>Whether the object was found and destroyed.</returns>
        bool DestroySpawnedObject (string path);
        /// <summary>
        /// Destroys all the previously spawned objects.
        /// </summary>
        void DestroyAllSpawnedObjects ();
        /// <summary>
        /// Checks whether an object with the provided path is currently spawned.
        /// </summary>
        bool IsObjectSpawned (string path);

        /// <summary>
        /// Preloads and holds resources required to spawn an object with the provided path.
        /// </summary>
        UniTask HoldResourcesAsync (object holder, string path);
        /// <summary>
        /// Releases resources required to spawn an object with the provided path.
        /// </summary>
        void ReleaseResources (object holder, string path);
    }
}
