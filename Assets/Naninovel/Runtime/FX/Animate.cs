// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Used by <see cref="AnimateActor"/> command.
    /// </summary>
    public class Animate : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable
    {
        protected virtual string ActorId { get; private set; }
        protected virtual bool Loop { get; private set; }
        protected virtual List<string> Appearance { get; } = new List<string>();
        protected virtual List<string> Transition { get; } = new List<string>();
        protected virtual List<bool?> Visibility { get; } = new List<bool?>();
        protected virtual List<float?> PositionX { get; } = new List<float?>();
        protected virtual List<float?> PositionY { get; } = new List<float?>();
        protected virtual List<float?> PositionZ { get; } = new List<float?>();
        protected virtual List<float?> RotationZ { get; } = new List<float?>();
        protected virtual List<float?> Scale { get; } = new List<float?>();
        protected virtual List<string> TintColor { get; } = new List<string>();
        protected virtual List<string> EasingTypeName { get; } = new List<string>();
        protected virtual List<float?> Duration { get; } = new List<float?>();

        protected virtual int KeyCount { get; private set; }
        protected virtual string SpawnedPath { get; private set; }
        protected virtual ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();

        private readonly List<UniTask> tasks = new List<UniTask>();

        public virtual void SetSpawnParameters (string[] parameters)
        {
            SpawnedPath = gameObject.name;
            KeyCount = 1 + parameters.Max(s => string.IsNullOrEmpty(s) ? 0 : s.Count(c => c == AnimateActor.KeyDelimiter));

            // Required paramaters.
            ActorId = parameters.ElementAtOrDefault(0);
            Loop = bool.Parse(parameters.ElementAtOrDefault(1));

            // Optional parameters.
            var cameraConfig = Engine.GetService<ICameraManager>().Configuration;
            for (int paramIdx = 2; paramIdx < 13; paramIdx++)
            {
                var keys = parameters.ElementAtOrDefault(paramIdx)?.Split(AnimateActor.KeyDelimiter);
                if (keys is null || keys.Length == 0 || keys.All(s => s == string.Empty)) continue;

                void AssignKeys<T> (List<T> parameter, Func<string, T> parseKey = default)
                {
                    var defaultKeys = Enumerable.Repeat<T>(default, KeyCount);
                    parameter.AddRange(defaultKeys);

                    for (int keyIdx = 0; keyIdx < keys.Length; keyIdx++)
                        if (!string.IsNullOrEmpty(keys[keyIdx]))
                            parameter[keyIdx] = parseKey is null ? (T)(object)keys[keyIdx] : parseKey(keys[keyIdx]);
                }

                if (paramIdx == 2) AssignKeys(Appearance);
                if (paramIdx == 3) AssignKeys(Transition);
                if (paramIdx == 4) AssignKeys(Visibility, k => bool.Parse(k));
                if (paramIdx == 5) AssignKeys(PositionX, k => cameraConfig.SceneToWorldSpace(new Vector2(k.AsInvariantFloat().Value / 100f, 0)).x);
                if (paramIdx == 6) AssignKeys(PositionY, k => cameraConfig.SceneToWorldSpace(new Vector2(0, k.AsInvariantFloat().Value / 100f)).y);
                if (paramIdx == 7) AssignKeys(PositionZ, k => k.AsInvariantFloat());
                if (paramIdx == 8) AssignKeys(RotationZ, k => k.AsInvariantFloat());
                if (paramIdx == 9) AssignKeys(Scale, k => k.AsInvariantFloat());
                if (paramIdx == 10) AssignKeys(TintColor);
                if (paramIdx == 11) AssignKeys(EasingTypeName);
                if (paramIdx == 12) AssignKeys(Duration, k => k.AsInvariantFloat());
            }

            // Fill missing durations.
            var lastDuration = 0f;
            for (int keyIdx = 0; keyIdx < KeyCount; keyIdx++)
                if (!Duration[keyIdx].HasValue)
                    Duration[keyIdx] = lastDuration;
                else lastDuration = Duration[keyIdx].Value;
        }

        public async UniTask AwaitSpawnAsync (CancellationToken cancellationToken = default)
        {
            var manager = Engine.GetAllServices<IActorManager>(c => c.ActorExists(ActorId)).FirstOrDefault();
            if (manager is null)
            {
                Debug.LogWarning($"Can't find a manager with `{ActorId}` actor to apply `{SpawnedPath}` command.");
                return;
            }
            var actor = manager.GetActor(ActorId);

            if (Loop)
            {
                while (Loop && Application.isPlaying && !cancellationToken.IsCancellationRequested)
                    for (int keyIdx = 0; keyIdx < KeyCount; keyIdx++)
                        await AnimateKey(actor, keyIdx, cancellationToken);
            }
            else
            {
                for (int keyIdx = 0; keyIdx < KeyCount; keyIdx++)
                    await AnimateKey(actor, keyIdx, cancellationToken);

                if (cancellationToken.IsCancellationRequested) return;
            }

            if (SpawnManager.IsObjectSpawned(SpawnedPath))
                SpawnManager.DestroySpawnedObject(SpawnedPath);
        }

        protected virtual async UniTask AnimateKey (IActor actor, int keyIndex, CancellationToken cancellationToken)
        {
            tasks.Clear();

            var duration = Duration[keyIndex].Value;
            var easingType = EasingType.Linear;
            if (EasingTypeName.ElementAtOrDefault(keyIndex) != null && !Enum.TryParse(EasingTypeName[keyIndex], true, out easingType))
                Debug.LogWarning($"Failed to parse `{EasingTypeName}` easing.");

            if (Appearance.ElementAtOrDefault(keyIndex) != null)
            {
                var transitionName = !string.IsNullOrEmpty(Transition.ElementAtOrDefault(keyIndex)) ? Transition[keyIndex] : TransitionType.Crossfade;
                var transition = new Transition(transitionName);
                tasks.Add(actor.ChangeAppearanceAsync(Appearance[keyIndex], duration, easingType, transition, cancellationToken));
            }

            if (Visibility.ElementAtOrDefault(keyIndex).HasValue)
                tasks.Add(actor.ChangeVisibilityAsync(Visibility[keyIndex].Value, duration, easingType, cancellationToken));

            if (PositionX.ElementAtOrDefault(keyIndex).HasValue || PositionY.ElementAtOrDefault(keyIndex).HasValue || PositionZ.ElementAtOrDefault(keyIndex).HasValue)
                tasks.Add(actor.ChangePositionAsync(new Vector3(
                    PositionX.ElementAtOrDefault(keyIndex) ?? actor.Position.x,
                    PositionY.ElementAtOrDefault(keyIndex) ?? actor.Position.y,
                    PositionZ.ElementAtOrDefault(keyIndex) ?? actor.Position.z), duration, easingType, cancellationToken));

            if (RotationZ.ElementAtOrDefault(keyIndex).HasValue)
                tasks.Add(actor.ChangeRotationZAsync(RotationZ[keyIndex].Value, duration, easingType, cancellationToken));

            if (Scale.ElementAtOrDefault(keyIndex).HasValue)
                tasks.Add(actor.ChangeScaleAsync(Vector3.one * Scale[keyIndex].Value, duration, easingType, cancellationToken));

            if (TintColor.ElementAtOrDefault(keyIndex) != null)
            {
                if (ColorUtility.TryParseHtmlString(TintColor[keyIndex], out var color))
                    tasks.Add(actor.ChangeTintColorAsync(color, duration, easingType, cancellationToken));
                else Debug.LogWarning($"Failed to parse `{TintColor}` color to apply tint animation for `{actor.Id}` actor. See the API docs for supported color formats.");
            }

            await UniTask.WhenAll(tasks);
        }

        private void OnDestroy ()
        {
            Loop = false;

            // The following is possible to prevent unpredicted state mutations,
            // though it's hardly worth all the complications:
            //   1. Add transient actor state (per each parameter), that is not serialied.
            //   2. When starting animation set real state to the last key frame.
            //   3. When any "normal" command modifies a property that has a transient state -- remove the transient effect.

            if (Engine.Initialized && SpawnManager.IsObjectSpawned(SpawnedPath))
                SpawnManager.DestroySpawnedObject(SpawnedPath);
        }
    }
}
