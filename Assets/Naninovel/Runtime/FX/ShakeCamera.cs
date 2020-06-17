// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes the main Naninovel render camera.
    /// </summary>
    public class ShakeCamera : ShakeTransform
    {
        protected override Transform GetShakedTransform ()
        {
            var cameraMngr = Engine.GetService<ICameraManager>().Camera;
            if (cameraMngr == null) return null;
            return cameraMngr.transform;
        }
    }
}
