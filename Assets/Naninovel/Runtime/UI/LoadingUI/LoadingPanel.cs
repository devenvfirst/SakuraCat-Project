// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.UI
{
    public class LoadingPanel : CustomUI, ILoadingUI
    {
        private IStateManager stateManager;
        private IInputManager inputManager;

        protected override void Awake ()
        {
            base.Awake();

            stateManager = Engine.GetService<IStateManager>();
            inputManager = Engine.GetService<IInputManager>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            stateManager.OnGameLoadStarted += HandleLoadStarted;
            stateManager.OnGameLoadFinished += HandleLoadFinished;
            stateManager.OnResetStarted += Show;
            stateManager.OnResetFinished += Hide;
            inputManager.AddBlockingUI(this);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (stateManager != null)
            {
                stateManager.OnGameLoadStarted -= HandleLoadStarted;
                stateManager.OnGameLoadFinished -= HandleLoadFinished;
                stateManager.OnResetStarted -= Show;
                stateManager.OnResetFinished -= Hide;
            }
            inputManager?.RemoveBlockingUI(this);
        }

        private void HandleLoadStarted (GameSaveLoadArgs args) => Show();
        private void HandleLoadFinished (GameSaveLoadArgs args) => Hide();
    }
}
