// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.UI
{
    public class ControlPanelTitleButton : ScriptableButton
    {
        [ManagedText("DefaultUI")]
        protected static string ConfirmationMessage = "Are you sure you want to quit to the title screen? Any unsaved game progress will be lost.";

        private IStateManager gameState;
        private IUIManager uiManager;
        private IConfirmationUI confirmationUI;

        protected override void Awake ()
        {
            base.Awake();

            gameState = Engine.GetService<IStateManager>();
            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void Start ()
        {
            base.Start();

            confirmationUI = uiManager.GetUI<IConfirmationUI>();
        }

        protected override void OnButtonClick ()
        {
            uiManager.GetUI<IPauseUI>()?.Hide();

            ExitToTitleAsync();
        }

        private async void ExitToTitleAsync ()
        {
            if (!await confirmationUI.ConfirmAsync(ConfirmationMessage)) return;

            await gameState.ResetStateAsync();
            uiManager.GetUI<ITitleUI>()?.Show();
        }
    } 
}
