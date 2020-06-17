// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="ISpawnManager"/>
    [InitializeAtRuntime]
    public class SpawnManager : IStatefulService<GameStateMap>, ISpawnManager
    {
        /// <summary>
        /// Used to delimit spawned object ID from its path.
        /// </summary>
        public const string IdDelimiter = "#";

        [System.Serializable]
        public class GameState 
        { 
            public List<SpawnedObjectState> SpawnedObjects; 
        }

        private class SpawnedObject 
        { 
            public GameObject Object; 
            public SpawnedObjectState State; 
        }

        public SpawnConfiguration Configuration { get; }

        private readonly List<SpawnedObject> spawnedObjects = new List<SpawnedObject>();
        private readonly IResourceProviderManager providersManager;
        private ResourceLoader<GameObject> loader;

        public SpawnManager (SpawnConfiguration config, IResourceProviderManager providersManager)
        {
            Configuration = config;
            this.providersManager = providersManager;
        }

        public UniTask InitializeServiceAsync ()
        {
            loader = Configuration.Loader.CreateFor<GameObject>(providersManager);
            return UniTask.CompletedTask;
        }

        public void ResetService ()
        {
            DestroyAllSpawnedObjects();
        }

        public void DestroyService ()
        {
            DestroyAllSpawnedObjects();
        }

        public void SaveServiceState (GameStateMap stateMap)
        {
            var state = new GameState() {
                SpawnedObjects = spawnedObjects.Select(o => o.State).ToList()
            };
            stateMap.SetState(state);
        }

        public UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state?.SpawnedObjects?.Count > 0)
            {
                if (spawnedObjects.Count > 0)
                    foreach (var obj in spawnedObjects.ToList())
                        if (!state.SpawnedObjects.Exists(o => o.Path.EqualsFast(obj.State.Path)))
                            DestroySpawnedObject(obj.State.Path);

                foreach (var objState in state.SpawnedObjects)
                    if (!IsObjectSpawned(objState.Path))
                        SpawnAsync(objState.Path, default, objState.Params).Forget();
                    else UpdateSpawnedAsync(objState.Path, default, objState.Params).Forget();
            }
            else if (spawnedObjects.Count > 0) DestroyAllSpawnedObjects();
            return UniTask.CompletedTask;
        }

        public async UniTask HoldResourcesAsync (object holder, string path)
        {
            var resourcePath = ProcessInputPath(path, out _);
            var resource = await loader.LoadAsync(resourcePath);
            if (resource.IsValid)
                resource.Hold(holder);
        }

        public void ReleaseResources (object holder, string path)
        {
            var resourcePath = ProcessInputPath(path, out _);
            if (!loader.IsLoaded(resourcePath)) return;

            var resource = loader.GetLoadedOrNull(resourcePath);
            resource.Release(holder, false);
            if (resource.HoldersCount == 0)
            {
                if (IsObjectSpawned(path))
                    DestroySpawnedObject(path);
                resource.Provider.UnloadResource(resource.Path);
            }
        }

        public async UniTask SpawnAsync (string path, CancellationToken cancellationToken = default, params string[] parameters)
        {
            if (IsObjectSpawned(path))
            {
                Debug.LogWarning($"Object `{path}` is already spawned and can't be spawned again before it's destroyed.");
                return;
            }

            var resourcePath = ProcessInputPath(path, out _);
            var prefabResource = await loader.LoadAsync(resourcePath);
            if (!prefabResource.IsValid)
            {
                Debug.LogWarning($"Failed to spawn `{resourcePath}`: resource is not valid.");
                return;
            }

            prefabResource.Hold(this);

            var obj = Engine.Instantiate(prefabResource.Object, path);

            var spawnedObj = new SpawnedObject { Object = obj, State = new SpawnedObjectState(path, parameters) };
            spawnedObjects.Add(spawnedObj);

            var parameterized = obj.GetComponent<Commands.Spawn.IParameterized>();
            if (parameterized != null) parameterized.SetSpawnParameters(parameters);

            var awaitable = obj.GetComponent<Commands.Spawn.IAwaitable>();
            if (awaitable != null) await awaitable.AwaitSpawnAsync(cancellationToken);
        }

        public async UniTask UpdateSpawnedAsync (string path, CancellationToken cancellationToken = default, params string[] parameters)
        {
            if (!IsObjectSpawned(path)) return;

            var spawnedData = GetSpawnedObject(path);
            spawnedData.State = new SpawnedObjectState(path, parameters);

            var parameterized = spawnedData.Object.GetComponent<Commands.Spawn.IParameterized>();
            if (parameterized != null) parameterized.SetSpawnParameters(parameters);

            var awaitable = spawnedData.Object.GetComponent<Commands.Spawn.IAwaitable>();
            if (awaitable != null) await awaitable.AwaitSpawnAsync(cancellationToken);
        }

        public async UniTask<bool> DestroySpawnedAsync (string path, CancellationToken cancellationToken = default, params string[] parameters)
        {
            var spawnedObj = GetSpawnedObject(path);
            if (spawnedObj is null)
            {
                Debug.LogWarning($"Failed to destroy spawned object `{path}`: the object is not found.");
                return false;
            }

            var parameterized = spawnedObj.Object.GetComponent<Commands.DestroySpawned.IParameterized>();
            if (parameterized != null) parameterized.SetDestroyParameters(parameters);

            var awaitable = spawnedObj.Object.GetComponent<Commands.DestroySpawned.IAwaitable>();
            if (awaitable != null) await awaitable.AwaitDestroyAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested) return false;

            return DestroySpawnedObject(path);
        }

        public bool DestroySpawnedObject (string path)
        {
            var spawnedObj = GetSpawnedObject(path);
            if (spawnedObj is null)
            {
                Debug.LogWarning($"Failed to destroy spawned object `{path}`: the object is not found.");
                return false;
            }

            var removed = spawnedObjects?.Remove(spawnedObj);
            ObjectUtils.DestroyOrImmediate(spawnedObj.Object);

            var resourcePath = ProcessInputPath(path, out _);
            loader.GetLoadedOrNull(resourcePath)?.Release(this);

            return removed ?? false;
        }

        public void DestroyAllSpawnedObjects ()
        {
            foreach (var spawnedObj in spawnedObjects)
                ObjectUtils.DestroyOrImmediate(spawnedObj.Object);
            spawnedObjects.Clear();

            loader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        public bool IsObjectSpawned (string path)
        {
            return spawnedObjects?.Exists(o => o.State.Path.EqualsFast(path)) ?? false;
        }

        private SpawnedObject GetSpawnedObject (string path)
        {
            return spawnedObjects?.FirstOrDefault(o => o.State.Path.EqualsFast(path));
        }

        /// <summary>
        /// In case <paramref name="input"/> contains <see cref="IdDelimiter"/>, 
        /// extracts ID and returns path without the ID and delimiter; otherwise, returns input.
        /// </summary>
        private string ProcessInputPath (string input, out string id)
        {
            if (input.Contains(IdDelimiter))
            {
                id = input.GetAfterFirst(IdDelimiter);
                return input.GetBefore(IdDelimiter);
            }

            id = null;
            return input;
        }
    }
}
