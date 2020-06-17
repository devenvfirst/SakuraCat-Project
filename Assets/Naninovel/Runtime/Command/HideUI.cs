// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Makes [UI elements](/guide/user-interface.md#ui-customization) with the specified names invisible.
    /// When no names are specified, will stop rendering (hide) the entire UI (including all the built-in UIs).
    /// </summary>
    /// <remarks>
    /// When hiding the entire UI with this command and `allowToggle` parameter is false (default), user won't be able to re-show the UI 
    /// back with hotkeys or by clicking anywhere on the screen; use [@showUI] command to make the UI ~~great~~ visible again.
    /// </remarks>
    /// <example>
    /// ; Given a custom `Calendar` UI, the following command will hide it.
    /// @hideUI Calendar
    /// 
    /// ; Hide the entire UI, won't allow user to re-show it
    /// @hideUI
    /// ...
    /// ; Make the UI visible again
    /// @showUI
    /// 
    /// ; Hide the entire UI, but allow the user to toggle it back
    /// @hideUI allowToggle:true
    /// 
    /// ; Simultaneously hide built-in `TipsUI` and custom `Calendar` UIs.
    /// @hideUI TipsUI,Calendar
    /// </example>
    public class HideUI : Command
    {
        /// <summary>
        /// Name of the UI elements to hide.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringListParameter UINames;
        /// <summary>
        /// When hiding the entire UI, controls whether to allow the user to re-show the UI with hotkeys or by clicking anywhere on the screen (false by default).
        /// Has no effect when hiding a particular UI.
        /// </summary>
        public BooleanParameter AllowToggle = false;
        /// <summary>
        /// Duration (in seconds) of the hide animation. 
        /// When not specified, will use UI-specific duration.
        /// </summary>
        [ParameterAlias("time")]
        public DecimalParameter Duration;

        private List<UniTask> changeVisibilityTasks = new List<UniTask>();

        public override void OnAfterDeserialize ()
        {
            base.OnAfterDeserialize();
            changeVisibilityTasks = new List<UniTask>();
        }

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            var uiManager = Engine.GetService<IUIManager>();

            if (!Assigned(UINames))
            {
                uiManager.SetUIVisibleWithToggle(false, AllowToggle);
                return;
            }

            changeVisibilityTasks.Clear();
            foreach (var name in UINames)
            {
                var ui = uiManager.GetUI(name);
                if (ui is null)
                {
                    LogWarningWithPosition($"Failed to hide `{name}` UI: managed UI with the specified prefab name not found.");
                    continue;
                }

                changeVisibilityTasks.Add(ui.ChangeVisibilityAsync(false, Assigned(Duration) ? Duration : null));
            }

            await UniTask.WhenAll(changeVisibilityTasks);
        }
    }
}
