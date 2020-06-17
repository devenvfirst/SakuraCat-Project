// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.UI
{
    public class TitleNewGameButton : ScriptableButton
    {
        private string startScriptName;
        private TitleMenu titleMenu;
        private IScriptPlayer player;
        private IStateManager stateManager;

        protected override void Awake ()
        {
            base.Awake();

            startScriptName = Engine.GetService<IScriptManager>().StartGameScriptName;
            titleMenu = GetComponentInParent<TitleMenu>();
            player = Engine.GetService<IScriptPlayer>();
            stateManager = Engine.GetService<IStateManager>();
            Debug.Assert(titleMenu && player != null);
        }

        protected override void Start ()
        {
            base.Start();

            if (string.IsNullOrEmpty(startScriptName))
                UIComponent.interactable = false;
        }

        protected override void OnButtonClick ()
        {
            if (string.IsNullOrEmpty(startScriptName))
            {
                Debug.LogError("Can't start new game: please specify start script name in the settings.");
                return;
            }

            titleMenu.Hide();
            StartNewGameAsync();
        }

        private async void StartNewGameAsync ()
        {
            await stateManager.ResetStateAsync(null, () => player.PreloadAndPlayAsync(startScriptName));
        }
    }
}
