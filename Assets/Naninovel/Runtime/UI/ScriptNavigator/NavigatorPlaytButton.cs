// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class NavigatorPlaytButton : ScriptableButton, IPointerEnterHandler, IPointerExitHandler
    {
        private Text labelText;
        private ScriptNavigatorPanel navigator;
        private Script script;
        private IScriptPlayer player;
        private IStateManager stateManager;
        private bool isInitialized;

        public virtual void Initialize (ScriptNavigatorPanel navigator, Script script, IScriptPlayer player)
        {
            this.navigator = navigator;
            this.script = script;
            this.player = player;
            name = "PlayScript: " + script.Name;
            if (labelText) labelText.text = script.Name;
            isInitialized = true;
            UIComponent.interactable = true;
        }

        public virtual void OnPointerEnter (PointerEventData eventData)
        {
            if (UIComponent.interactable)
                labelText.fontStyle = FontStyle.Bold;
        }

        public virtual void OnPointerExit (PointerEventData eventData)
        {
            labelText.fontStyle = FontStyle.Normal;
        }

        protected override void Awake ()
        {
            base.Awake();

            labelText = GetComponentInChildren<Text>();
            labelText.text = "Loading...";
            UIComponent.interactable = false;

            stateManager = Engine.GetService<IStateManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            stateManager.GameStateSlotManager.OnBeforeLoad += ControlInteractability;
            stateManager.GameStateSlotManager.OnLoaded += ControlInteractability;
            stateManager.GameStateSlotManager.OnBeforeSave += ControlInteractability;
            stateManager.GameStateSlotManager.OnSaved += ControlInteractability;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            stateManager.GameStateSlotManager.OnBeforeLoad -= ControlInteractability;
            stateManager.GameStateSlotManager.OnLoaded -= ControlInteractability;
            stateManager.GameStateSlotManager.OnBeforeSave -= ControlInteractability;
            stateManager.GameStateSlotManager.OnSaved -= ControlInteractability;
        }

        protected override void OnButtonClick ()
        {
            Debug.Assert(isInitialized);
            navigator.Hide();
            Engine.GetService<IUIManager>()?.GetUI<ITitleUI>()?.Hide();
            PlayScriptAsync();
        }

        private async void PlayScriptAsync ()
        {
            await stateManager.ResetStateAsync(null, () => player.PreloadAndPlayAsync(script.Name));
        }

        private void ControlInteractability ()
        {
            UIComponent.interactable = !stateManager.GameStateSlotManager.Loading && !stateManager.GameStateSlotManager.Saving;
        }
    } 
}
