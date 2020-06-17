// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IActor"/> implementation using <typeparamref name="TBehaviour"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// Resource prefab should have a <typeparamref name="TBehaviour"/> component attached to the root object.
    /// Apperance and other property changes changes are routed to the events of the <typeparamref name="TBehaviour"/> component.
    /// </remarks>
    public abstract class GenericActor<TBehaviour> : MonoBehaviourActor
        where TBehaviour : GenericActorBehaviour
    {
        public override string Appearance { get => appearance; set => SetAppearance(value); }
        public override bool Visible { get => visible; set => SetVisibility(value); }

        protected TBehaviour Behaviour { get; private set; }

        private ActorMetadata metadata;
        private LocalizableResourceLoader<GameObject> prefabLoader;
        private string appearance;
        private bool visible;
        private Color tintColor = Color.white;

        public GenericActor (string id, ActorMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerMngr = Engine.GetService<IResourceProviderManager>();
            var localeMngr = Engine.GetService<ILocalizationManager>();
            prefabLoader = metadata.Loader.CreateLocalizableFor<GameObject>(providerMngr, localeMngr);
            var prefabResource = await prefabLoader.LoadAsync(Id);

            Behaviour = Engine.Instantiate(prefabResource.Object).GetComponent<TBehaviour>();
            Behaviour.transform.SetParent(Transform);

            SetVisibility(false);
        }

        public override UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            SetAppearance(appearance);
            return UniTask.CompletedTask;
        }

        public override UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            SetVisibility(visible);
            return UniTask.CompletedTask;
        }

        protected virtual void SetAppearance (string appearance)
        {
            this.appearance = appearance;

            if (string.IsNullOrEmpty(appearance))
                return;

            Behaviour.InvokeAppearanceChangedEvent(appearance);
        }

        protected virtual void SetVisibility (bool visible)
        {
            this.visible = visible;

            Behaviour.InvokeVisibilityChangedEvent(visible);
        }

        protected override Color GetBehaviourTintColor () => tintColor;

        protected override void SetBehaviourTintColor (Color tintColor)
        {
            this.tintColor = tintColor;

            Behaviour.InvokeTintColorChangedEvent(tintColor);
        }

        public override void Dispose ()
        {
            base.Dispose();

            prefabLoader?.UnloadAll();
        }
    }
}
