// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="Transform"/>.
    /// </summary>
    public abstract class ShakeTransform : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable
    {
        public string SpawnedPath { get; private set; }
        public string ObjectName { get; private set; }
        public int ShakesCount { get; private set; }
        public float ShakeDuration { get; private set; }
        public float DurationVariation { get; private set; }
        public float ShakeAmplitude { get; private set; }
        public float AmplitudeVariation { get; private set; }
        public bool ShakeHorizontally { get; private set; }
        public bool ShakeVertically { get; private set; }

        protected ISpawnManager SpawnManager => Engine.GetService<ISpawnManager>();
        protected Vector3 DeltaPos { get; private set; }
        protected Vector3 InitialPos { get; private set; }
        protected Transform ShakedTransform { get; private set; }
        protected bool Loop { get; private set; }

        [SerializeField] private int defaultShakesCount = 3;
        [SerializeField] private float defaultShakeDuration = .15f;
        [SerializeField] private float defaultDurationVariation = .25f;
        [SerializeField] private float defaultShakeAmplitude = .5f;
        [SerializeField] private float defaultAmplitudeVariation = .5f;
        [SerializeField] private bool defaultShakeHorizontally = false;
        [SerializeField] private bool defaultShakeVertically = true;

        private readonly Tweener<VectorTween> positionTweener = new Tweener<VectorTween>();

        public virtual void SetSpawnParameters (string[] parameters)
        {
            if (positionTweener.Running)
                positionTweener.CompleteInstantly();
            if (ShakedTransform != null)
                ShakedTransform.position = InitialPos;

            SpawnedPath = gameObject.name;
            ObjectName = parameters?.ElementAtOrDefault(0);
            ShakesCount = Mathf.Abs(parameters?.ElementAtOrDefault(1)?.AsInvariantInt() ?? defaultShakesCount);
            ShakeDuration = Mathf.Abs(parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultShakeDuration);
            DurationVariation = Mathf.Clamp01(parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultDurationVariation);
            ShakeAmplitude = Mathf.Abs(parameters?.ElementAtOrDefault(4)?.AsInvariantFloat() ?? defaultShakeAmplitude);
            AmplitudeVariation = Mathf.Clamp01(parameters?.ElementAtOrDefault(5)?.AsInvariantFloat() ?? defaultAmplitudeVariation);
            ShakeHorizontally = bool.Parse(parameters?.ElementAtOrDefault(6) ?? defaultShakeHorizontally.ToString());
            ShakeVertically = bool.Parse(parameters?.ElementAtOrDefault(7) ?? defaultShakeVertically.ToString());
            Loop = ShakesCount <= 0;
        }

        public virtual async UniTask AwaitSpawnAsync (CancellationToken cancellationToken = default)
        {
            ShakedTransform = GetShakedTransform();
            if (ShakedTransform == null)
            {
                SpawnManager.DestroySpawnedObject(SpawnedPath);
                Debug.LogWarning($"Failed to apply `{GetType().Name}` FX to `{ObjectName}`: game object not found.");
                return;
            }

            InitialPos = ShakedTransform.position;
            DeltaPos = new Vector3(ShakeHorizontally ? ShakeAmplitude : 0, ShakeVertically ? ShakeAmplitude : 0, 0);

            if (Loop)
            {
                while (Loop && Application.isPlaying && !cancellationToken.IsCancellationRequested)
                    await ShakeSequenceAsync(cancellationToken);
            }
            else
            {
                for (int i = 0; i < ShakesCount; i++)
                    await ShakeSequenceAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested) return;

                if (SpawnManager.IsObjectSpawned(SpawnedPath))
                    SpawnManager.DestroySpawnedObject(SpawnedPath);
            }

            await AsyncUtils.WaitEndOfFrame; // Otherwise a consequent shake won't work.
        }

        protected abstract Transform GetShakedTransform ();

        protected virtual async UniTask ShakeSequenceAsync (CancellationToken cancellationToken)
        {
            var amplitude = DeltaPos + DeltaPos * Random.Range(-AmplitudeVariation, AmplitudeVariation);
            var duration = ShakeDuration + ShakeDuration * Random.Range(-DurationVariation, DurationVariation);

            await MoveAsync(InitialPos - amplitude * .5f, duration * .25f, cancellationToken);
            await MoveAsync(InitialPos + amplitude, duration * .5f, cancellationToken);
            await MoveAsync(InitialPos, duration * .25f, cancellationToken);
        }

        protected virtual async UniTask MoveAsync (Vector3 position, float duration, CancellationToken cancellationToken)
        {
            var tween = new VectorTween(ShakedTransform.position, position, duration, pos => ShakedTransform.position = pos, false, EasingType.SmoothStep, ShakedTransform);
            await positionTweener.RunAsync(tween, cancellationToken);
        }

        protected virtual void OnDestroy ()
        {
            Loop = false;

            if (ShakedTransform != null)
                ShakedTransform.position = InitialPos;

            if (Engine.Initialized && SpawnManager.IsObjectSpawned(SpawnedPath))
                SpawnManager.DestroySpawnedObject(SpawnedPath);
        }
    }
}
