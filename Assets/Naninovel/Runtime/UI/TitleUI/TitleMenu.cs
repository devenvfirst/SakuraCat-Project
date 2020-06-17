// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UniRx.Async;
using UnityEngine;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TitleMenu : CustomUI, ITitleUI
    {
        private IScriptPlayer scriptPlayer;
        private IStateManager stateManager;
        private string titleScriptName;

        protected override void Awake ()
        {
            base.Awake();

            scriptPlayer = Engine.GetService<IScriptPlayer>();
            stateManager = Engine.GetService<IStateManager>();
            titleScriptName = Engine.GetConfiguration<ScriptsConfiguration>().TitleScript;
        }

        public override async UniTask ChangeVisibilityAsync (bool visible, float? duration = null)
        {
            if (visible && !string.IsNullOrEmpty(titleScriptName))
            {
                await scriptPlayer.PreloadAndPlayAsync(titleScriptName);
                while (scriptPlayer.Playing) await AsyncUtils.WaitEndOfFrame;
            }

            var changeVisibilityTask = base.ChangeVisibilityAsync(visible, duration);

            // Save title menu visibility state; otherwise it's lost when changing language, 
            // while a title or initialization scripts are used (the game is save-loaded in those cases).
            var stateMap = stateManager.PeekRollbackStack();
            if (stateMap != null && stateMap.GetState<GameState>(name) is GameState state)
                state.Visible = visible;

            await changeVisibilityTask;
        }
    }
}
