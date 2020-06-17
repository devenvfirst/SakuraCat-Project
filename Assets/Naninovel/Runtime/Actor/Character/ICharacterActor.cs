// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to represent a character actor on scene.
    /// </summary>
    public interface ICharacterActor : IActor
    {
        /// <summary>
        /// Look direction of the actor.
        /// </summary>
        CharacterLookDirection LookDirection { get; set; }

        /// <summary>
        /// Changes character look direction over specified time using provided animation easing.
        /// </summary>
        UniTask ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration, EasingType easingType = default, CancellationToken cancellationToken = default);
    } 
}
