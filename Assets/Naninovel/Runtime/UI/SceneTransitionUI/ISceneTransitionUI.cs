// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.UI
{
    /// <summary>
    /// Handles scene transition (<see cref="Commands.StartSceneTransition"/> and <see cref="Commands.FinishSceneTransition"/> commands).
    /// </summary>
    public interface ISceneTransitionUI : IManagedUI 
    {
        /// <summary>
        /// Saves the current main camera content to a temporary render texture to use during the transition.
        /// </summary>
        void CaptureScene ();
        /// <summary>
        /// Performs transition between the previously captured scene texture and current main camera content.
        /// </summary>
        UniTask TransitionAsync (Transition transition, float duration, EasingType easingType = default, CancellationToken cancellationToken = default);
    }
}
