// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IChoiceHandlerActor"/> implementation using <see cref="UI.ChoiceHandlerPanel"/> to represent the actor.
    /// </summary>
    public class UIChoiceHandler : MonoBehaviourActor, IChoiceHandlerActor
    {
        public override string Appearance { get; set; }
        public override bool Visible { get => HandlerPanel.Visible; set => HandlerPanel.Visible = value; }
        public List<ChoiceState> Choices { get; } = new List<ChoiceState>();

        protected ChoiceHandlerPanel HandlerPanel { get; private set; }

        private readonly IStateManager stateManager;
        private ChoiceHandlerMetadata metadata;

        public UIChoiceHandler (string id, ChoiceHandlerMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;

            stateManager = Engine.GetService<IStateManager>();
        }

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerMngr = Engine.GetService<IResourceProviderManager>();
            var prefabResource = await metadata.Loader.CreateFor<GameObject>(providerMngr).LoadAsync(Id);
            if (!prefabResource.IsValid)
            {
                Debug.LogError($"Failed to load `{Id}` choice handler resource object. Make sure the handler is correctly configured.");
                return;
            }

            var uiMngr = Engine.GetService<IUIManager>();
            HandlerPanel = await uiMngr.InstantiatePrefabAsync(prefabResource.Object) as ChoiceHandlerPanel;
            HandlerPanel.OnChoice += HandleChoice;
            HandlerPanel.transform.SetParent(Transform);

            Visible = false;
        }

        public override UniTask ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default,
            Transition? transition = default, CancellationToken cancellationToken = default)
        {
            return UniTask.CompletedTask;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            if (HandlerPanel)
                await HandlerPanel.ChangeVisibilityAsync(visible, duration);
        }

        public virtual void AddChoice (ChoiceState choice)
        {
            Choices.Add(choice);
            HandlerPanel.AddChoiceButton(choice);
        }

        public virtual void RemoveChoice (string id)
        {
            Choices.RemoveAll(c => c.Id == id);
            HandlerPanel.RemoveChoiceButton(id);
        }

        public ChoiceState GetChoice (string id) => Choices.FirstOrDefault(c => c.Id == id);

        protected override Color GetBehaviourTintColor () => Color.white;

        protected override void SetBehaviourTintColor (Color tintColor) { }

        protected async void HandleChoice (ChoiceState state)
        {
            if (!Choices.Exists(c => c.Id.EqualsFast(state.Id))) return;

            stateManager.PeekRollbackStack()?.AllowPlayerRollback();

            Choices.Clear();

            if (HandlerPanel)
            {
                HandlerPanel.RemoveAllChoiceButtonsDelayed(); // Delayed to allow custom onClick logic.
                HandlerPanel.Hide();
            }

            var script = Script.FromScriptText($"`{Id}` on choice script", state.OnSelectScript);
            var playlist = new ScriptPlaylist(script);
            foreach (var command in playlist)
                if (command.ShouldExecute)
                {
                    if (!command.Wait && !(command is Commands.Command.IForceWait))
                        command.ExecuteAsync().Forget();
                    else await command.ExecuteAsync();
                }

            var player = Engine.GetService<IScriptPlayer>();
            if (state.AutoPlay && !player.Playing)
            {
                var nextIndex = player.PlayedIndex + 1;
                player.Play(player.Playlist, nextIndex);
                return;
            }
        }
    } 
}
