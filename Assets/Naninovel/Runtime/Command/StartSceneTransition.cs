// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Begins scene transition masking the real scene content with anything that is visible at the moment (except the UI).
    /// When the new scene is ready, finish with [@finishTrans] command.
    /// </summary>
    /// <remarks>
    /// The UI will be hidden and user input blocked while the transition is in progress. 
    /// You can change that by overriding the `ISceneTransitionUI`, which handles the transition process.<br/><br/>
    /// For the list of available transition effect options see [transition effects](/guide/transition-effects.md) guide.
    /// </remarks>
    /// <example>
    /// ; Transition Felix on sunny day with Jenna on rainy day
    /// @char Felix
    /// @back SunnyDay
    /// @fx SunShafts
    /// @startTrans
    /// ; The following modifications won't be visible until we finish the transition
    /// @hideChars time:0
    /// @char Jenna time:0
    /// @back RainyDay time:0
    /// @stopFx SunShafts params:0
    /// @fx Rain params:,0
    /// ; Transition the initially captured scene to the new one with `DropFade` effect over 3 seconds
    /// @finishTrans DropFade time:3
    /// </example>
    [CommandAlias("startTrans")]
    public class StartSceneTransition : Command
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            Engine.GetService<IUIManager>().GetUI<UI.ISceneTransitionUI>()?.CaptureScene();
            return UniTask.CompletedTask;
        }
    }
}
