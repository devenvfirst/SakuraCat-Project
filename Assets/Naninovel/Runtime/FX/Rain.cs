// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    [RequireComponent(typeof(ParticleSystem))]
    public class Rain : MonoBehaviour, Spawn.IParameterized, Spawn.IAwaitable, DestroySpawned.IParameterized, DestroySpawned.IAwaitable
    {
        protected float Intensity { get; private set; }
        protected float FadeInTime { get; private set; }
        protected float FadeOutTime { get; private set; }

        [SerializeField] private float defaultIntensity = 500f;
        [SerializeField] private float defaultFadeInTime = 5f;
        [SerializeField] private float defaultFadeOutTime = 5f;
        [SerializeField] private float defaultVelocityX = 1f;
        [SerializeField] private float defaultVelocityY = 1f;

        private readonly Tweener<FloatTween> intensityTweener = new Tweener<FloatTween>();
        private ParticleSystem particles;
        private ParticleSystem.EmissionModule emissionModule;
        private ParticleSystem.VelocityOverLifetimeModule velocityModule;

        public virtual void SetSpawnParameters (string[] parameters)
        {
            Intensity = parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultIntensity;
            FadeInTime = Mathf.Abs(parameters?.ElementAtOrDefault(1)?.AsInvariantFloat() ?? defaultFadeInTime);
            velocityModule.xMultiplier *= parameters?.ElementAtOrDefault(2)?.AsInvariantFloat() ?? defaultVelocityX;
            velocityModule.yMultiplier *= parameters?.ElementAtOrDefault(3)?.AsInvariantFloat() ?? defaultVelocityY;
        }

        public async UniTask AwaitSpawnAsync (CancellationToken cancellationToken = default)
        {
            if (intensityTweener.Running)
                intensityTweener.CompleteInstantly();

            var tween = new FloatTween(emissionModule.rateOverTimeMultiplier, Intensity, FadeInTime, SetIntensity, target: particles);
            await intensityTweener.RunAsync(tween, cancellationToken);
        }

        public void SetDestroyParameters (string[] parameters)
        {
            FadeOutTime = Mathf.Abs(parameters?.ElementAtOrDefault(0)?.AsInvariantFloat() ?? defaultFadeOutTime);
        }

        public async UniTask AwaitDestroyAsync (CancellationToken cancellationToken = default)
        {
            if (intensityTweener.Running)
                intensityTweener.CompleteInstantly();

            var tween = new FloatTween(emissionModule.rateOverTimeMultiplier, 0, FadeOutTime, SetIntensity, target: particles);
            await intensityTweener.RunAsync(tween, cancellationToken);
        }

        private void Awake ()
        {
            particles = GetComponent<ParticleSystem>();
            emissionModule = particles.emission;
            velocityModule = particles.velocityOverLifetime;

            SetIntensity(0);
        }

        private void Start ()
        {
            // Position before the first background.
            transform.position = new Vector3(0, 0, Engine.GetConfiguration<BackgroundsConfiguration>().ZOffset - 1);
        }

        private void SetIntensity (float value)
        {
            emissionModule.rateOverTimeMultiplier = value;
        }
    }
}
