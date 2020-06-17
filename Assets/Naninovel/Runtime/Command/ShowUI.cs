// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Makes [UI elements](/guide/user-interface.md) with the specified prefab names visible.
    /// When no names are specified, will reveal the entire UI (in case it was hidden with [@hideUI]).
    /// </summary>
    /// <example>
    /// ; Given you've added a custom UI with prefab name `Calendar`,
    /// ; the following will make it visible on the scene.
    /// @showUI Calendar
    /// 
    /// ; Given you've hide the entire UI with @hideUI, show it back
    /// @showUI
    /// 
    /// ; Simultaneously reveal built-in `TipsUI` and custom `Calendar` UIs.
    /// @showUI TipsUI,Calendar
    /// </example>
    public class ShowUI : Command
    {
        /// <summary>
        /// Name of the UI prefab to make visible.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias)]
        public StringListParameter UINames;
        /// <summary>
        /// Duration (in seconds) of the show animation. 
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
                uiManager.SetUIVisibleWithToggle(true);
                return;
            }

            changeVisibilityTasks.Clear();
            foreach (var name in UINames)
            {
                var ui = uiManager.GetUI(name);
                if (ui is null)
                {
                    LogWarningWithPosition($"Failed to show `{name}` UI: managed UI with the specified prefab name not found.");
                    continue;
                }
                changeVisibilityTasks.Add(ui.ChangeVisibilityAsync(true, Assigned(Duration) ? Duration : null));
            }

            await UniTask.WhenAll(changeVisibilityTasks);
        }
    }
}
