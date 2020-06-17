// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IStateManager"/>
    [InitializeAtRuntime(1)] // Here settings for all the other services will be applied, so initialize at the end.
    public class StateManager : IStateManager
    {
        public event Action<GameSaveLoadArgs> OnGameLoadStarted;
        public event Action<GameSaveLoadArgs> OnGameLoadFinished;
        public event Action<GameSaveLoadArgs> OnGameSaveStarted;
        public event Action<GameSaveLoadArgs> OnGameSaveFinished;
        public event Action OnResetStarted;
        public event Action OnResetFinished;
        public event Action OnRollbackStarted;
        public event Action OnRollbackFinished;

        public StateConfiguration Configuration { get; }
        public GlobalStateMap GlobalState { get; private set; }
        public SettingsStateMap SettingsState { get; private set; }
        public ISaveSlotManager<GlobalStateMap> GlobalStateSlotManager { get; }
        public ISaveSlotManager<GameStateMap> GameStateSlotManager { get; }
        public ISaveSlotManager<SettingsStateMap> SettingsSlotManager { get; }
        public bool QuickLoadAvailable => GameStateSlotManager.SaveSlotExists(LastQuickSaveSlotId);
        public bool AnyGameSaveExists => GameStateSlotManager.AnySaveExists();
        public bool RollbackInProgress => rollbackTaskQueue.Count > 0;

        protected string LastQuickSaveSlotId => Configuration.IndexToQuickSaveSlotId(1);

        private readonly StateRollbackStack rollbackStack;
        private readonly Queue<GameStateMap> rollbackTaskQueue = new Queue<GameStateMap>();
        private readonly List<Action<GameStateMap>> onGameSerializeTasks = new List<Action<GameStateMap>>();
        private readonly List<Func<GameStateMap, UniTask>> onGameDeserializeTasks = new List<Func<GameStateMap, UniTask>>();
        private IInputSampler rollbackInput;

        public StateManager (StateConfiguration config, EngineConfiguration engineConfig)
        {
            Configuration = config;

            var rollbackCapacity = (config.EnableStateRollback || Application.isEditor || Debug.isDebugBuild) ? Mathf.Max(1, config.StateRollbackSteps) : 1; // One step is reserved for game save operations.
            rollbackStack = new StateRollbackStack(rollbackCapacity);

            var savesFolderPath = PathUtils.Combine(engineConfig.GeneratedDataPath, config.SaveFolderName);
            GameStateSlotManager = (ISaveSlotManager<GameStateMap>)Activator.CreateInstance(Type.GetType(config.GameStateHandler), config, savesFolderPath);
            GlobalStateSlotManager = (ISaveSlotManager<GlobalStateMap>)Activator.CreateInstance(Type.GetType(config.GlobalStateHandler), config, savesFolderPath);
            SettingsSlotManager = (ISaveSlotManager<SettingsStateMap>)Activator.CreateInstance(Type.GetType(config.SettingsStateHandler), config, savesFolderPath);

            Engine.AddPostInitializationTask(InitializeStateAsync);
        }

        public UniTask InitializeServiceAsync ()
        {
            Engine.GetService<IScriptPlayer>().AddPreExecutionTask(HandleCommandPreExecution);

            rollbackInput = Engine.GetService<IInputManager>().GetRollback();

            if (rollbackInput != null)
                rollbackInput.OnStart += HandleRollbackInputStart;

            return UniTask.CompletedTask;
        }

        public void ResetService ()
        {
            rollbackStack.Clear();
        }

        public void DestroyService ()
        {
            Engine.GetService<IScriptPlayer>()?.RemovePreExecutionTask(HandleCommandPreExecution);

            if (rollbackInput != null)
                rollbackInput.OnStart -= HandleRollbackInputStart;

            Engine.RemovePostInitializationTask(InitializeStateAsync);
        }

        public void AddOnGameSerializeTask (Action<GameStateMap> task) => onGameSerializeTasks.Insert(0, task);

        public void RemoveOnGameSerializeTask (Action<GameStateMap> task) => onGameSerializeTasks.Remove(task);

        public void AddOnGameDeserializeTask (Func<GameStateMap, UniTask> task) => onGameDeserializeTasks.Insert(0, task);

        public void RemoveOnGameDeserializeTask (Func<GameStateMap, UniTask> task) => onGameDeserializeTasks.Remove(task);

        public async UniTask<GameStateMap> SaveGameAsync (string slotId)
        {
            if (rollbackStack.Count == 0 || rollbackStack.Peek() is null)
                PushRollbackSnapshot(false);

            var quick = slotId.StartsWithFast(Configuration.QuickSaveSlotMask.GetBefore("{"));

            OnGameSaveStarted?.Invoke(new GameSaveLoadArgs(slotId, quick));

            var state = new GameStateMap(rollbackStack.Peek());
            state.SaveDateTime = DateTime.Now;
            state.Thumbnail = Engine.GetService<ICameraManager>().CaptureThumbnail();

            var lastZero = rollbackStack.FirstOrDefault(s => s.PlaybackSpot.InlineIndex == 0); // Required when changing locale.
            bool filter (GameStateMap s) => (Configuration.EnableStateRollback && Configuration.SavedRollbackSteps > 0 && s.PlayerRollbackAllowed) || s == lastZero;
            state.RollbackStackJson = rollbackStack.ToJson(Configuration.SavedRollbackSteps, filter);

            await GameStateSlotManager.SaveAsync(slotId, state);

            // Also save global state on every game save.
            await SaveGlobalStateAsync();

            OnGameSaveFinished?.Invoke(new GameSaveLoadArgs(slotId, quick));

            return state;
        }

        public async UniTask<GameStateMap> QuickSaveAsync ()
        {
            // Free first quick save slot by shifting existing ones by one.
            for (int i = Configuration.QuickSaveSlotLimit; i > 0; i--)
            {
                var curSlotId = Configuration.IndexToQuickSaveSlotId(i);
                var prevSlotId = Configuration.IndexToQuickSaveSlotId(i + 1);
                GameStateSlotManager.RenameSaveSlot(curSlotId, prevSlotId);
            }

            // Delete the last slot in case it's out of the limit.
            var outOfLimitSlotId = Configuration.IndexToQuickSaveSlotId(Configuration.QuickSaveSlotLimit + 1);
            if (GameStateSlotManager.SaveSlotExists(outOfLimitSlotId))
                GameStateSlotManager.DeleteSaveSlot(outOfLimitSlotId);

            var firstSlotId = string.Format(Configuration.QuickSaveSlotMask, 1);
            return await SaveGameAsync(firstSlotId);
        }

        public async UniTask<GameStateMap> LoadGameAsync (string slotId)
        {
            if (string.IsNullOrEmpty(slotId) || !GameStateSlotManager.SaveSlotExists(slotId))
            {
                Debug.LogError($"Slot '{slotId}' not found when loading '{typeof(GameStateMap)}' data.");
                return null;
            }

            var quick = slotId.EqualsFast(LastQuickSaveSlotId);

            OnGameLoadStarted?.Invoke(new GameSaveLoadArgs(slotId, quick));

            if (Configuration.LoadStartDelay > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(Configuration.LoadStartDelay));

            Engine.Reset();
            await Resources.UnloadUnusedAssets();

            var state = await GameStateSlotManager.LoadAsync(slotId) as GameStateMap;
            await LoadAllServicesFromStateAsync<IStatefulService<GameStateMap>, GameStateMap>(state);

            if (Configuration.EnableStateRollback && Configuration.SavedRollbackSteps > 0)
                rollbackStack.OverrideFromJson(state.RollbackStackJson, s => s.AllowPlayerRollback());

            for (int i = onGameDeserializeTasks.Count - 1; i >= 0; i--)
                await onGameDeserializeTasks[i](state);

            OnGameLoadFinished?.Invoke(new GameSaveLoadArgs(slotId, quick));

            return state;
        }

        public async UniTask<GameStateMap> QuickLoadAsync () => await LoadGameAsync(LastQuickSaveSlotId);

        public async UniTask<GlobalStateMap> SaveGlobalStateAsync ()
        {
            SaveAllServicesToState<IStatefulService<GlobalStateMap>, GlobalStateMap>(GlobalState);
            await GlobalStateSlotManager.SaveAsync(Configuration.DefaultGlobalSlotId, GlobalState);
            return GlobalState;
        }

        public async UniTask<SettingsStateMap> SaveSettingsAsync ()
        {
            SaveAllServicesToState<IStatefulService<SettingsStateMap>, SettingsStateMap>(SettingsState);
            await SettingsSlotManager.SaveAsync(Configuration.DefaultSettingsSlotId, SettingsState);
            return SettingsState;
        }

        public async UniTask ResetStateAsync (string[] exclude = null, params Func<UniTask>[] tasks)
        {
            OnResetStarted?.Invoke();

            if (Configuration.LoadStartDelay > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(Configuration.LoadStartDelay));

            Engine.GetService<IScriptPlayer>()?.Playlist?.ReleaseResources();

            if (exclude != null && exclude.Length > 0)
                Engine.Reset(exclude.Select(typeName => Type.GetType($"Naninovel.{typeName}", true, true)).ToArray());
            else Engine.Reset();

            await Resources.UnloadUnusedAssets();

            if (tasks != null)
            {
                foreach (var task in tasks)
                    await task?.Invoke();
            }

            OnResetFinished?.Invoke();
        }

        public void PushRollbackSnapshot (bool allowPlayerRollback)
        {
            var state = new GameStateMap();
            state.SaveDateTime = DateTime.Now;
            state.PlayerRollbackAllowed = allowPlayerRollback;

            SaveAllServicesToState<IStatefulService<GameStateMap>, GameStateMap>(state);

            for (int i = onGameSerializeTasks.Count - 1; i >= 0; i--)
                onGameSerializeTasks[i](state);

            rollbackStack.Push(state);
        }

        public async UniTask<bool> RollbackAsync (Predicate<GameStateMap> predicate)
        {
            var state = rollbackStack.Pop(predicate);
            if (state is null) return false;

            await RollbackToStateAsync(state);
            return true;
        }

        public async UniTask<bool> RollbackAsync ()
        {
            if (rollbackStack.Capacity <= 1) return false;

            var state = rollbackStack.Pop();
            if (state is null) return false;

            await RollbackToStateAsync(state);
            return true;
        }

        public GameStateMap PeekRollbackStack () => rollbackStack?.Peek();

        public bool CanRollbackTo (Predicate<GameStateMap> predicate) => rollbackStack.Contains(predicate);

        public void PurgeRollbackData () => rollbackStack.ForEach(s => s.PlayerRollbackAllowed = false);

        private async UniTask InitializeStateAsync ()
        {
            SettingsState = await LoadSettingsAsync();
            GlobalState = await LoadGlobalStateAsync();
        }

        private async UniTask RollbackToStateAsync (GameStateMap state)
        {
            rollbackTaskQueue.Enqueue(state);
            OnRollbackStarted?.Invoke();

            while (rollbackTaskQueue.Peek() != state)
                await AsyncUtils.WaitEndOfFrame;

            await LoadAllServicesFromStateAsync<IStatefulService<GameStateMap>, GameStateMap>(state);

            for (int i = onGameDeserializeTasks.Count - 1; i >= 0; i--)
                await onGameDeserializeTasks[i](state);

            rollbackTaskQueue.Dequeue();
            OnRollbackFinished?.Invoke();
        }

        private async UniTask<GlobalStateMap> LoadGlobalStateAsync ()
        {
            var stateData = await GlobalStateSlotManager.LoadOrDefaultAsync(Configuration.DefaultGlobalSlotId);
            await LoadAllServicesFromStateAsync<IStatefulService<GlobalStateMap>, GlobalStateMap>(stateData);
            return stateData;
        }

        private async UniTask<SettingsStateMap> LoadSettingsAsync ()
        {
            var settingsData = await SettingsSlotManager.LoadOrDefaultAsync(Configuration.DefaultSettingsSlotId);
            await LoadAllServicesFromStateAsync<IStatefulService<SettingsStateMap>, SettingsStateMap>(settingsData);
            return settingsData;
        }

        private void SaveAllServicesToState<TService, TState> (TState state) 
            where TService : class, IStatefulService<TState>
            where TState : StateMap, new()
        {
            foreach (var service in Engine.GetAllServices<TService>())
                service.SaveServiceState(state);
        }

        private async UniTask LoadAllServicesFromStateAsync<TService, TState> (TState state)
            where TService : class, IStatefulService<TState>
            where TState : StateMap, new()
        {
            foreach (var service in Engine.GetAllServices<TService>())
                await service.LoadServiceStateAsync(state);
        }

        private async void HandleRollbackInputStart ()
        {
            if (!Configuration.EnableStateRollback || !CanRollbackTo(s => s.PlayerRollbackAllowed)) return;

            await RollbackAsync(s => s.PlayerRollbackAllowed);
        }

        private UniTask HandleCommandPreExecution (Commands.Command _)
        {
            PushRollbackSnapshot(false);
            return UniTask.CompletedTask;
        }
    } 
}
