// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops playback of the currently played voice clip.
    /// </summary>
    public class StopVoice : AudioCommand
    {
        public override UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            AudioManager.StopVoice();
            return UniTask.CompletedTask;
        }
    } 
}
