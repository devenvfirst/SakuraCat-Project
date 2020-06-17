// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.UI
{
    public class ControlPanelHideButton : ScriptableLabeledButton
    {
        private IUIManager uiManager;

        protected override void Awake ()
        {
            base.Awake();

            uiManager = Engine.GetService<IUIManager>();
        }

        protected override void OnButtonClick () => uiManager.SetUIVisibleWithToggle(false);
    } 
}
