// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptPlayer"/>
    [InitializeAtRuntime]
    public class ScriptPlayer : IStatefulService<SettingsStateMap>, IStatefulService<GlobalStateMap>, IStatefulService<GameStateMap>, IScriptPlayer
    {
        [Serializable]
        public class Settings
        {
            public PlayerSkipMode SkipMode = PlayerSkipMode.ReadOnly;
        }

        [Serializable]
        public class GlobalState
        {
            public PlayedScriptRegister PlayedScriptRegister = new PlayedScriptRegister();
        }

        [Serializable]
        public class GameState
        {
            public bool Playing;
            public bool WaitingForInput;
            public List<PlaybackSpot> GosubReturnSpots;
        }

        public event Action<Script> OnPlay;
        public event Action<Script> OnStop;
        public event Action<Command> OnCommandExecutionStart;
        public event Action<Command> OnCommandExecutionFinish;
        public event Action<bool> OnSkip;
        public event Action<bool> OnAutoPlay;
        public event Action<bool> OnWaitingForInput;

        public ScriptPlayerConfiguration Configuration { get; }
        public bool Playing => playRoutineCTS != null;
        public bool SkipAllowed => GetSkipAllowed();
        public bool SkipActive { get; private set; }
        public bool AutoPlayActive { get; private set; }
        public bool WaitingForInput { get; private set; }
        public PlayerSkipMode SkipMode { get; set; }
        public Script PlayedScript { get; private set; }
        public Command PlayedCommand => Playlist?.GetCommandByIndex(PlayedIndex);
        public PlaybackSpot PlaybackSpot => PlayedCommand?.PlaybackSpot ?? default;
        public ScriptPlaylist Playlist { get; private set; }
        public int PlayedIndex { get; private set; }
        public Stack<PlaybackSpot> GosubReturnSpots { get; private set; }
        public int PlayedCommandsCount => playedScriptRegister.CountPlayed();

        private readonly ResourceProviderConfiguration providerConfig;
        private readonly IInputManager inputManager;
        private readonly IScriptManager scriptManager;
        private readonly IStateManager stateManager;
        private readonly List<Func<Command, UniTask>> preExecutionTasks = new List<Func<Command, UniTask>>();
        private readonly List<Func<Command, UniTask>> postExecutionTasks = new List<Func<Command, UniTask>>();
        private PlayedScriptRegister playedScriptRegister;
        private CancellationTokenSource playRoutineCTS;
        private CancellationTokenSource commandExecutionCTS;
        private UniTaskCompletionSource<object> waitForWaitForInputDisabledTCS;
        private IInputSampler continueInput, skipInput, autoPlayInput;

        public ScriptPlayer (ScriptPlayerConfiguration config, ResourceProviderConfiguration providerConfig, 
            IScriptManager scriptManager, IInputManager inputManager, IStateManager stateManager)
        {
            Configuration = config;
            this.providerConfig = providerConfig;
            this.scriptManager = scriptManager;
            this.inputManager = inputManager;
            this.stateManager = stateManager;

            GosubReturnSpots = new Stack<PlaybackSpot>();
            playedScriptRegister = new PlayedScriptRegister();
            commandExecutionCTS = new CancellationTokenSource();
        }

        public UniTask InitializeServiceAsync ()
        {
            continueInput = inputManager.GetContinue();
            skipInput = inputManager.GetSkip();
            autoPlayInput = inputManager.GetAutoPlay();

            if (continueInput != null)
            {
                continueInput.OnStart += DisableWaitingForInput;
                continueInput.OnStart += DisableSkip;
            }
            if (skipInput != null)
            {
                skipInput.OnStart += EnableSkip;
                skipInput.OnEnd += DisableSkip;
            }
            if (autoPlayInput != null)
                autoPlayInput.OnStart += ToggleAutoPlay;

            if (Configuration.ShowDebugOnInit)
                UI.DebugInfoGUI.Toggle();

            return UniTask.CompletedTask;
        }

        public void ResetService ()
        {
            Stop();
            CancelCommands();
            // Playlist?.ReleaseResources(); performed in StateManager; 
            // here it could be invoked after the actors are already destroyed.
            Playlist = null;
            PlayedIndex = -1;
            PlayedScript = null;
            DisableWaitingForInput();
            DisableAutoPlay();
            DisableSkip();
        }

        public void DestroyService ()
        {
            ResetService();

            commandExecutionCTS?.Dispose();

            if (continueInput != null)
            {
                continueInput.OnStart -= DisableWaitingForInput;
                continueInput.OnStart -= DisableSkip;
            }
            if (skipInput != null)
            {
                skipInput.OnStart -= EnableSkip;
                skipInput.OnEnd -= DisableSkip;
            }
            if (autoPlayInput != null)
                autoPlayInput.OnStart -= ToggleAutoPlay;
        }

        public void SaveServiceState (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                SkipMode = SkipMode
            };
            stateMap.SetState(settings);
        }

        public UniTask LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings();
            SkipMode = settings.SkipMode;
            return UniTask.CompletedTask;
        }

        public void SaveServiceState (GlobalStateMap stateMap)
        {
            var globalState = new GlobalState {
                PlayedScriptRegister = playedScriptRegister
            };
            stateMap.SetState(globalState);
        }

        public UniTask LoadServiceStateAsync (GlobalStateMap stateMap)
        {
            var state = stateMap.GetState<GlobalState>() ?? new GlobalState();
            playedScriptRegister = state.PlayedScriptRegister;
            return UniTask.CompletedTask;
        }

        public void SaveServiceState (GameStateMap stateMap)
        {
            var gameState = new GameState() {
                Playing = Playing,
                WaitingForInput = WaitingForInput,
                GosubReturnSpots = GosubReturnSpots.Count > 0 ? GosubReturnSpots.Reverse().ToList() : null // Stack is reversed on enum.
            };
            stateMap.PlaybackSpot = PlaybackSpot;
            stateMap.SetState(gameState);
        }

        public async UniTask LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                ResetService();
                return;
            }

            // Force stop and cancel all running commands to prevent state mutation while loading other services.
            Stop(); CancelCommands();

            if (state.Playing) // The playback is resumed (when necessary) after other services are loaded.
            {
                if (stateManager.RollbackInProgress) stateManager.OnRollbackFinished += PlayAfterRollback;
                else stateManager.OnGameLoadFinished += PlayAfterLoad;
            }

            SetWaitingForInputEnabled(state.WaitingForInput);
            if (state.GosubReturnSpots != null && state.GosubReturnSpots.Count > 0)
                GosubReturnSpots = new Stack<PlaybackSpot>(state.GosubReturnSpots);
            else GosubReturnSpots.Clear();

            if (!string.IsNullOrEmpty(stateMap.PlaybackSpot.ScriptName))
            {
                if (PlayedScript is null || !stateMap.PlaybackSpot.ScriptName.EqualsFast(PlayedScript.Name))
                {
                    PlayedScript = await scriptManager.LoadScriptAsync(stateMap.PlaybackSpot.ScriptName);
                    Playlist = new ScriptPlaylist(PlayedScript, scriptManager);
                    PlayedIndex = Playlist.IndexOf(stateMap.PlaybackSpot);
                    Debug.Assert(PlayedIndex >= 0, $"Failed to load script player state: `{stateMap.PlaybackSpot}` doesn't exist in the current playlist.");
                    var endIndex = providerConfig.ResourcePolicy == ResourcePolicy.Static ? Playlist.Count - 1 :
                        Mathf.Min(PlayedIndex + providerConfig.DynamicPolicySteps, Playlist.Count - 1);
                    await Playlist.HoldResourcesAsync(PlayedIndex, endIndex);
                }
                else PlayedIndex = Playlist.IndexOf(stateMap.PlaybackSpot);
            }
            else
            {
                Playlist?.Clear();
                PlayedScript = null;
                PlayedIndex = 0;
            }

            void PlayAfterRollback ()
            {
                stateManager.OnRollbackFinished -= PlayAfterRollback;
                Play();
            }

            void PlayAfterLoad (GameSaveLoadArgs _)
            {
                stateManager.OnGameLoadFinished -= PlayAfterLoad;
                Play();
            }
        }

        public void AddPreExecutionTask (Func<Command, UniTask> taskFunc) => preExecutionTasks.Insert(0, taskFunc);

        public void RemovePreExecutionTask (Func<Command, UniTask> taskFunc) => preExecutionTasks.Remove(taskFunc);

        public void AddPostExecutionTask (Func<Command, UniTask> taskFunc) => postExecutionTasks.Insert(0, taskFunc);

        public void RemovePostExecutionTask (Func<Command, UniTask> taskFunc) => postExecutionTasks.Remove(taskFunc);

        public void Play ()
        {
            if (PlayedScript is null || Playlist is null)
            {
                Debug.LogError("Failed to start script playback: the script is not set.");
                return;
            }

            if (Playing) Stop();

            if (Playlist.IsIndexValid(PlayedIndex) || SelectNextCommand())
            {
                playRoutineCTS = new CancellationTokenSource();
                PlayRoutineAsync(playRoutineCTS.Token).Forget();
                OnPlay?.Invoke(PlayedScript);
            }
        }

        public void Play (ScriptPlaylist playlist, int playlistIndex)
        {
            Playlist = playlist;
            PlayedIndex = playlistIndex;
            Play();
        }

        public void Play (Script script, int startLineIndex = 0, int startInlineIndex = 0)
        {
            PlayedScript = script;

            if (Playlist is null || Playlist.ScriptName != script.Name)
                Playlist = new ScriptPlaylist(script, scriptManager);

            if (startLineIndex > 0 || startInlineIndex > 0)
            {
                var startCommand = Playlist.GetCommandAfterLine(startLineIndex, startInlineIndex);
                if (startCommand is null) { Debug.LogError($"Script player failed to start: no commands found in script `{PlayedScript.Name}` at line #{startLineIndex}.{startInlineIndex}."); return; }
                PlayedIndex = Playlist.IndexOf(startCommand);
            }
            else PlayedIndex = 0;

            Play();
        }

        public async UniTask PreloadAndPlayAsync (string scriptName, int startLineIndex = 0, int startInlineIndex = 0, string label = null)
        {
            var script = await scriptManager.LoadScriptAsync(scriptName);
            if (script is null)
            {
                Debug.LogError($"Script player failed to start: script with name `{scriptName}` wasn't able to load.");
                return;
            }

            if (!string.IsNullOrEmpty(label))
            {
                if (!script.LabelExists(label))
                {
                    Debug.LogError($"Failed navigating script playback to `{label}` label: label not found in `{script.Name}` script.");
                    return;
                }
                startLineIndex = script.GetLineIndexForLabel(label);
                startInlineIndex = 0;
            }

            Playlist = new ScriptPlaylist(script, scriptManager);
            var startAction = Playlist.GetCommandAfterLine(startLineIndex, startInlineIndex);
            var startIndex = startAction != null ? Playlist.IndexOf(startAction) : 0;

            var endIndex = providerConfig.ResourcePolicy == ResourcePolicy.Static ? Playlist.Count - 1 :
                Mathf.Min(startIndex + providerConfig.DynamicPolicySteps, Playlist.Count - 1);
            await Playlist.HoldResourcesAsync(startIndex, endIndex);

            Play(script, startLineIndex, startInlineIndex);
        }

        public void Stop ()
        {
            if (Playing)
            {
                playRoutineCTS.Cancel();
                playRoutineCTS.Dispose();
                playRoutineCTS = null;

                OnStop?.Invoke(PlayedScript);
            }

            DisableWaitingForInput();
        }

        public async UniTask<bool> RewindAsync (int lineIndex)
        {
            if (PlayedCommand is null)
            {
                Debug.LogError("Script player failed to rewind: played command is not valid.");
                return false;
            }

            var targetCommand = Playlist.GetCommandAfterLine(lineIndex, 0);
            if (targetCommand is null)
            {
                Debug.LogError($"Script player failed to rewind: target line index ({lineIndex}) is not valid for `{PlayedScript.Name}` script.");
                return false;
            }

            var targetPlaylistIndex = Playlist.IndexOf(targetCommand);
            if (targetPlaylistIndex == PlayedIndex)
                return true;

            if (Playing) Stop();

            var wasWaitingInput = WaitingForInput;
            DisableAutoPlay();
            DisableSkip();
            DisableWaitingForInput();

            playRoutineCTS = new CancellationTokenSource();
            var cancellationToken = playRoutineCTS.Token;

            bool result;
            if (targetPlaylistIndex > PlayedIndex)
            {
                // In case were waiting input, the current command wasn't executed; execute it now.
                result = await FastForwardRoutineAsync(cancellationToken, targetPlaylistIndex, wasWaitingInput);
                Play();
            }
            else
            {
                var targetSpot = new PlaybackSpot(PlayedScript.Name, lineIndex, 0);
                result = await stateManager.RollbackAsync(s => s.PlaybackSpot == targetSpot);
            }

            return result;
        }

        public void SetSkipEnabled (bool enable)
        {
            if (SkipActive == enable) return;
            if (enable && !SkipAllowed) return;

            SkipActive = enable;
            Time.timeScale = enable ? Configuration.SkipTimeScale : 1f;
            OnSkip?.Invoke(enable);

            if (enable && WaitingForInput) SetWaitingForInputEnabled(false);
        }

        public void SetAutoPlayEnabled (bool enable)
        {
            if (AutoPlayActive == enable) return;
            AutoPlayActive = enable;
            OnAutoPlay?.Invoke(enable);

            if (enable && WaitingForInput) SetWaitingForInputEnabled(false);
        }

        public void SetWaitingForInputEnabled (bool enable)
        {
            if (WaitingForInput == enable) return;

            if (SkipActive && enable || (!enable && (continueInput.Active || AutoPlayActive)))
                stateManager.PeekRollbackStack()?.AllowPlayerRollback();

            if (SkipActive && enable) return;

            WaitingForInput = enable;
            if (!enable)
            {
                waitForWaitForInputDisabledTCS?.TrySetResult(null);
                waitForWaitForInputDisabledTCS = null;
            }

            OnWaitingForInput?.Invoke(enable);
        }

        private void EnableSkip () => SetSkipEnabled(true);
        private void DisableSkip () => SetSkipEnabled(false);
        private void EnableAutoPlay () => SetAutoPlayEnabled(true);
        private void DisableAutoPlay () => SetAutoPlayEnabled(false);
        private void ToggleAutoPlay () => SetAutoPlayEnabled(!AutoPlayActive);
        private void EnableWaitingForInput () => SetWaitingForInputEnabled(true);
        private void DisableWaitingForInput () => SetWaitingForInputEnabled(false);

        private bool GetSkipAllowed ()
        {
            if (SkipMode == PlayerSkipMode.Everything) return true;
            if (PlayedScript is null) return false;
            return playedScriptRegister.IsIndexPlayed(PlayedScript.Name, PlayedIndex);
        }

        private async UniTask WaitForWaitForInputDisabledAsync ()
        {
            if (waitForWaitForInputDisabledTCS is null)
                waitForWaitForInputDisabledTCS = new UniTaskCompletionSource<object>();
            await waitForWaitForInputDisabledTCS.Task;
        }

        private async UniTask WaitForAutoPlayDelayAsync ()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Configuration.MinAutoPlayDelay));
            if (!AutoPlayActive) await WaitForWaitForInputDisabledAsync(); // In case auto play was disabled while waiting for delay.
        }

        private async UniTask ExecutePlayedCommandAsync ()
        {
            if (PlayedCommand is null || !PlayedCommand.ShouldExecute) return;

            OnCommandExecutionStart?.Invoke(PlayedCommand);

            playedScriptRegister.RegisterPlayedIndex(PlayedScript.Name, PlayedIndex);

            for (int i = preExecutionTasks.Count - 1; i >= 0; i--)
                await preExecutionTasks[i](PlayedCommand);

            if (PlayedCommand.Wait || PlayedCommand is Command.IForceWait) 
                await PlayedCommand.ExecuteAsync(commandExecutionCTS.Token);
            else PlayedCommand.ExecuteAsync(commandExecutionCTS.Token).Forget();

            for (int i = postExecutionTasks.Count - 1; i >= 0; i--)
                await postExecutionTasks[i](PlayedCommand);

            if (providerConfig.ResourcePolicy == ResourcePolicy.Dynamic)
            {
                if (PlayedCommand is Command.IPreloadable playedPreloadableCmd)
                    playedPreloadableCmd.ReleaseResources();
                // TODO: Handle @goto, @if/else/elseif and all the conditionally executed actions. (just unload everything that has a lower play index?)
                if (Playlist.GetCommandByIndex(PlayedIndex + providerConfig.DynamicPolicySteps) is Command.IPreloadable nextPreloadableCmd)
                    nextPreloadableCmd.HoldResourcesAsync().Forget();
            }

            OnCommandExecutionFinish?.Invoke(PlayedCommand);
        }

        private async UniTask PlayRoutineAsync (CancellationToken cancellationToken)
        {
            while (Engine.Initialized && Playing)
            {
                if (WaitingForInput)
                {
                    if (AutoPlayActive) 
                    { 
                        await UniTask.WhenAny(WaitForAutoPlayDelayAsync(), WaitForWaitForInputDisabledAsync()); 
                        DisableWaitingForInput(); 
                    }
                    else await WaitForWaitForInputDisabledAsync();
                    if (cancellationToken.IsCancellationRequested) break;
                }

                await ExecutePlayedCommandAsync();

                if (cancellationToken.IsCancellationRequested) break;

                var nextActionAvailable = SelectNextCommand();
                if (!nextActionAvailable) break;

                if (SkipActive && !SkipAllowed) SetSkipEnabled(false);
            }
        }

        private async UniTask<bool> FastForwardRoutineAsync (CancellationToken cancellationToken, int targetPlaylistIndex, bool executePlayedCommand)
        {
            SetSkipEnabled(true);

            if (executePlayedCommand)
                await ExecutePlayedCommandAsync();

            var reachedLine = true;
            while (Engine.Initialized && Playing)
            {
                var nextCommandAvailable = SelectNextCommand();
                if (!nextCommandAvailable) { reachedLine = false; break; }

                if (PlayedIndex >= targetPlaylistIndex) { reachedLine = true; break; }

                await ExecutePlayedCommandAsync();
                SetSkipEnabled(true); // Force skip mode to be always active while fast-forwarding.

                if (cancellationToken.IsCancellationRequested) { reachedLine = false; break; }
            }

            SetSkipEnabled(false);
            return reachedLine;
        }

        /// <summary>
        /// Attempts to select next <see cref="Command"/> in the current <see cref="Playlist"/>.
        /// </summary>
        /// <returns>Whether next command is available and was selected.</returns>
        private bool SelectNextCommand ()
        {
            PlayedIndex++;
            if (Playlist.IsIndexValid(PlayedIndex)) return true;

            // No commands left in the played script.
            Debug.Log($"Script '{PlayedScript.Name}' has finished playing, and there wasn't a follow-up goto command. " +
                        "Consider using stop command in case you wish to gracefully stop script execution.");
            Stop();
            return false;
        }

        /// <summary>
        /// Cancels all the asynchronously-running commands.
        /// </summary>
        /// <remarks>
        /// Be aware that this could lead to an inconsistent state; only use when the current engine state is going to be discarded 
        /// (eg, when preparing to load a game or perform state rollback).
        /// </remarks>
        private void CancelCommands ()
        {
            commandExecutionCTS.Cancel();
            commandExecutionCTS.Dispose();
            commandExecutionCTS = new CancellationTokenSource();
        }
    } 
}
