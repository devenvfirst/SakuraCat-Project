// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="IBackgroundActor"/> or the main one.
    /// </summary>
    public class ShakeBackground : ShakeTransform
    {
        protected override Transform GetShakedTransform ()
        {
            var id = string.IsNullOrEmpty(ObjectName) ? BackgroundsConfiguration.MainActorId : ObjectName;
            var go = GameObject.Find(id);
            return ObjectUtils.IsValid(go) ? go.transform : null;
        }
    }
}
