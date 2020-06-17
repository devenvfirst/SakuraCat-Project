// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.UI
{
    public class GameSettingsFontSizeSlider : ScriptableSlider
    {
        [ManagedText("DefaultUI")]
        protected static string RestartMessage = "Please restart the game to revert to the original font size.";

        private const int defaultFontSize = 25;

        private IUIManager uiManager;
        private IConfirmationUI confirmationUI;

        protected override void Awake ()
        {
            base.Awake();

            uiManager = Engine.GetService<IUIManager>();
            confirmationUI = uiManager.GetUI<IConfirmationUI>();
        }

        protected override void Start ()
        {
            base.Start();

            UIComponent.value = uiManager.FontSize ?? 0;
        }

        protected override void OnValueChanged (float value)
        {
            uiManager.FontSize = value == 0 ? null : (int?)value;

            if (value == 0)
            {
                uiManager.FontSize = defaultFontSize;
                uiManager.FontSize = null;
                confirmationUI?.NotifyAsync(RestartMessage);
            }
        }
    }
}
