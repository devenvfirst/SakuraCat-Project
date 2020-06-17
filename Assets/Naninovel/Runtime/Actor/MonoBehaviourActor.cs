// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <see cref="MonoBehaviour"/> to represent the actor.
    /// </summary>
    public abstract class MonoBehaviourActor : IActor, IDisposable
    {
        public virtual string Id { get; }
        public abstract string Appearance { get; set; }
        public abstract bool Visible { get; set; }
        public virtual Vector3 Position
        {
            get => position;
            set { CompletePositionTween(); position = value; SetBehaviourPosition(value); }
        }
        public virtual Quaternion Rotation
        {
            get => rotation;
            set { CompleteRotationTween(); rotation = value; SetBehaviourRotation(value); }
        }
        public virtual Vector3 Scale
        {
            get => scale;
            set { CompleteScaleTween(); scale = value; SetBehaviourScale(value); }
        }
        public virtual Color TintColor
        {
            get => tintColor;
            set { CompleteTintColorTween(); tintColor = value; SetBehaviourTintColor(value); }
        }

        protected virtual GameObject GameObject { get; private set; }
        protected virtual Transform Transform => GameObject.transform;

        private readonly Tweener<VectorTween> positionTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> rotationTweener = new Tweener<VectorTween>();
        private readonly Tweener<VectorTween> scaleTweener = new Tweener<VectorTween>();
        private readonly Tweener<ColorTween> tintColorTweener = new Tweener<ColorTween>();
        private Vector3 position = Vector3.zero;
        private Vector3 scale = Vector3.one;
        private Quaternion rotation = Quaternion.identity;
        private Color tintColor = Color.white;

        public MonoBehaviourActor (string id, ActorMetadata metadata)
        {
            Id = id;
            GameObject = Engine.CreateObject(id);
        }

        public virtual UniTask InitializeAsync () => UniTask.CompletedTask; 

        public abstract UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default);

        public abstract UniTask ChangeVisibilityAsync (bool isVisible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default);

        public virtual async UniTask ChangePositionAsync (Vector3 position, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompletePositionTween();
            this.position = position;

            var tween = new VectorTween(GetBehaviourPosition(), position, duration, SetBehaviourPosition, false, easingType, GameObject);
            await positionTweener.RunAsync(tween, cancellationToken);
        }

        public virtual async UniTask ChangeRotationAsync (Quaternion rotation, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteRotationTween();
            this.rotation = rotation;

            var tween = new VectorTween(GetBehaviourRotation().ClampedEulerAngles(), rotation.ClampedEulerAngles(), duration, SetBehaviourRotation, false, easingType, GameObject);
            await rotationTweener.RunAsync(tween, cancellationToken);
        }

        public virtual async UniTask ChangeScaleAsync (Vector3 scale, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteScaleTween();
            this.scale = scale;

            var tween = new VectorTween(GetBehaviourScale(), scale, duration, SetBehaviourScale, false, easingType, GameObject);
            await scaleTweener.RunAsync(tween, cancellationToken);
        }

        public virtual async UniTask ChangeTintColorAsync (Color tintColor, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            CompleteTintColorTween();
            this.tintColor = tintColor;

            var tween = new ColorTween(GetBehaviourTintColor(), tintColor, ColorTweenMode.All, duration, SetBehaviourTintColor, false, easingType, GameObject);
            await tintColorTweener.RunAsync(tween, cancellationToken);
        }

        public virtual UniTask HoldResourcesAsync (object holder, string appearance) => UniTask.CompletedTask;

        public virtual void ReleaseResources (object holder, string appearance) { }

        public virtual void Dispose () => ObjectUtils.DestroyOrImmediate(GameObject);

        protected virtual Vector3 GetBehaviourPosition () => Transform.position;
        protected virtual void SetBehaviourPosition (Vector3 position) => Transform.position = position;
        protected virtual Quaternion GetBehaviourRotation () => Transform.rotation;
        protected virtual void SetBehaviourRotation (Quaternion rotation) => Transform.rotation = rotation;
        protected virtual void SetBehaviourRotation (Vector3 rotation) => SetBehaviourRotation(Quaternion.Euler(rotation));
        protected virtual Vector3 GetBehaviourScale () => Transform.localScale;
        protected virtual void SetBehaviourScale (Vector3 scale) => Transform.localScale = scale;
        protected abstract Color GetBehaviourTintColor ();
        protected abstract void SetBehaviourTintColor (Color tintColor);

        private void CompletePositionTween ()
        {
            if (positionTweener.Running)
                positionTweener.CompleteInstantly();
        }

        private void CompleteRotationTween ()
        {
            if (rotationTweener.Running)
                rotationTweener.CompleteInstantly();
        }

        private void CompleteScaleTween ()
        {
            if (scaleTweener.Running)
                scaleTweener.CompleteInstantly();
        }

        private void CompleteTintColorTween ()
        {
            if (tintColorTweener.Running)
                tintColorTweener.CompleteInstantly();
        }
    }
}
