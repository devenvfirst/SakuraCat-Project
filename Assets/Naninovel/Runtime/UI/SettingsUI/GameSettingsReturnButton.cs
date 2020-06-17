// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.UI
{
    public class GameSettingsReturnButton : ScriptableButton
    {
        private GameSettingsMenu settingsMenu;
        private IStateManager settingsManager;

        protected override void Awake ()
        {
            base.Awake();

            settingsMenu = GetComponentInParent<GameSettingsMenu>();
            settingsManager = Engine.GetService<IStateManager>();
        }

        protected override void OnButtonClick () => ApplySettingsAsync();

        private async void ApplySettingsAsync ()
        {
            settingsMenu.SetInteractable(false);
            await settingsManager.SaveSettingsAsync();
            settingsMenu.SetInteractable(true);
            settingsMenu.Hide();
        }
    }
}
