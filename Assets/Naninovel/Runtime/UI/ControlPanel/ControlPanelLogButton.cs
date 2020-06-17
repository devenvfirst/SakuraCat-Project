// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.UI
{
    public class ControlPanelLogButton : ScriptableLabeledButton
    {
        private IUIManager uiManager;

        protected override void Awake ()
        {
            base.Awake();

            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void OnButtonClick ()
        {
            uiManager.GetUI<IPauseUI>()?.Hide();
            uiManager.GetUI<IBacklogUI>()?.Show();
        }
    } 
}
