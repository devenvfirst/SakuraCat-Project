// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="ICharacterActor"/> with provided name or a random visible one.
    /// </summary>
    public class ShakeCharacter : ShakeTransform
    {
        [SerializeField] private bool preventPositiveYOffset = true;

        protected override Transform GetShakedTransform ()
        {
            var mngr = Engine.GetService<ICharacterManager>();
            var id = string.IsNullOrEmpty(ObjectName) ? mngr.GetAllActors().FirstOrDefault(a => a.Visible)?.Id : ObjectName;
            var go = GameObject.Find(id);
            return ObjectUtils.IsValid(go) ? go.transform : null;
        }

        protected override async UniTask ShakeSequenceAsync (CancellationToken cancellationToken)
        {
            if (!preventPositiveYOffset)
            {
                await base.ShakeSequenceAsync(cancellationToken);
                return;
            }

            var amplitude = DeltaPos + DeltaPos * Random.Range(-AmplitudeVariation, AmplitudeVariation);
            var duration = ShakeDuration + ShakeDuration * Random.Range(-DurationVariation, DurationVariation);

            await MoveAsync(InitialPos - amplitude * .5f, duration * .5f, cancellationToken);
            await MoveAsync(InitialPos, duration * .5f, cancellationToken);
        }
    }
}
