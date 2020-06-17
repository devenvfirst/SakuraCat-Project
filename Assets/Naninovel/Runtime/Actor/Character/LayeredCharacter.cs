// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="LayeredActorBehaviour"/> to represent the actor.
    /// </summary>
    public class LayeredCharacter : LayeredActor, ICharacterActor
    {
        public CharacterLookDirection LookDirection { get => GetLookDirection(); set => SetLookDirection(value); }

        private readonly CharacterMetadata metadata;

        public LayeredCharacter (string id, CharacterMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;
        }
      
        public UniTask ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            SetLookDirection(lookDirection);
            return UniTask.CompletedTask;
        }

        protected virtual void SetLookDirection (CharacterLookDirection lookDirection)
        {
            if (metadata.BakedLookDirection == CharacterLookDirection.Center) return;
            if (lookDirection == CharacterLookDirection.Center)
            {
                SpriteRenderer.FlipX = false;
                return;
            }
            if (lookDirection != LookDirection)
                SpriteRenderer.FlipX = !SpriteRenderer.FlipX;
        }

        protected virtual CharacterLookDirection GetLookDirection ()
        {
            switch (metadata.BakedLookDirection)
            {
                case CharacterLookDirection.Center:
                    return CharacterLookDirection.Center;
                case CharacterLookDirection.Left:
                    return SpriteRenderer.FlipX ? CharacterLookDirection.Right : CharacterLookDirection.Left;
                case CharacterLookDirection.Right:
                    return SpriteRenderer.FlipX ? CharacterLookDirection.Left : CharacterLookDirection.Right;
                default:
                    return default;
            }
        }
    }
}
