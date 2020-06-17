// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="ICharacterActor"/> implementation using <see cref="CharacterActorBehaviour"/> to represent the actor.
    /// </summary>
    /// <remarks>
    /// Resource prefab should have a <see cref="CharacterActorBehaviour"/> component attached to the root object.
    /// Apperance and other property changes are routed via the events of <see cref="CharacterActorBehaviour"/> component.
    /// </remarks>
    public class GenericCharacter : GenericActor<CharacterActorBehaviour>, ICharacterActor, Commands.LipSync.IReceiver
    {
        public CharacterLookDirection LookDirection { get => lookDirection; set => SetLookDirection(value); }

        private readonly ITextPrinterManager textPrinterManager;
        private readonly IAudioManager audioManager;
        private CharacterLookDirection lookDirection;
        private bool lipSyncAllowed = true;

        public GenericCharacter (string id, CharacterMetadata metadata)
            : base(id, metadata)
        {
            textPrinterManager = Engine.GetService<ITextPrinterManager>();
            textPrinterManager.OnPrintTextStarted += HandlePrintTextStarted;
            audioManager = Engine.GetService<IAudioManager>();
        }

        public async override UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            Behaviour.InvokeIsSpeakingChangedEvent(false);
        }

        public async UniTask ChangeLookDirectionAsync (CharacterLookDirection lookDirection, float duration, 
            EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            this.lookDirection = lookDirection;

            Behaviour.InvokeLookDirectionChangedEvent(lookDirection);

            if (Behaviour.TransformByLookDirection)
            {
                var rotation = LookDirectionToRotation(lookDirection);
                await ChangeRotationAsync(rotation, duration, easingType, cancellationToken);
            }
        }

        public override void Dispose ()
        {
            base.Dispose();

            if (textPrinterManager != null)
            {
                textPrinterManager.OnPrintTextStarted -= HandlePrintTextStarted;
                textPrinterManager.OnPrintTextFinished -= HandlePrintTextFinished;
            }
        }

        public void AllowLipSync (bool active)
        {
            lipSyncAllowed = active;
        }

        protected virtual void SetLookDirection (CharacterLookDirection lookDirection)
        {
            this.lookDirection = lookDirection;

            Behaviour.InvokeLookDirectionChangedEvent(lookDirection);

            if (Behaviour.TransformByLookDirection)
            {
                var rotation = LookDirectionToRotation(lookDirection);
                SetBehaviourRotation(rotation);
            }
        }

        protected virtual Quaternion LookDirectionToRotation (CharacterLookDirection lookDirection)
        {
            var yAngle = 0f;
            switch (lookDirection)
            {
                case CharacterLookDirection.Center:
                    yAngle = 0;
                    break;
                case CharacterLookDirection.Left:
                    yAngle = Behaviour.LookDeltaAngle;
                    break;
                case CharacterLookDirection.Right:
                    yAngle = -Behaviour.LookDeltaAngle;
                    break;
            }

            var currentRotation = Rotation.eulerAngles;
            return Quaternion.Euler(currentRotation.x, yAngle, currentRotation.z);
        }

        private void HandlePrintTextStarted (PrintTextArgs args)
        {
            if (!lipSyncAllowed || args.AuthorId != Id) return;

            Behaviour.InvokeIsSpeakingChangedEvent(true);

            var playedVoicePath = audioManager.GetPlayedVoicePath();
            if (!string.IsNullOrEmpty(playedVoicePath))
            {
                var track = audioManager.GetVoiceTrack(playedVoicePath);
                track.OnStop -= HandleVoiceClipStopped;
                track.OnStop += HandleVoiceClipStopped;
            }
            else textPrinterManager.OnPrintTextFinished += HandlePrintTextFinished;
        }

        private void HandlePrintTextFinished (PrintTextArgs args)
        {
            if (args.AuthorId != Id) return;
            
            Behaviour.InvokeIsSpeakingChangedEvent(false);
            textPrinterManager.OnPrintTextFinished -= HandlePrintTextFinished;
        }

        private void HandleVoiceClipStopped ()
        {
            Behaviour.InvokeIsSpeakingChangedEvent(false);
        }
    }
}
