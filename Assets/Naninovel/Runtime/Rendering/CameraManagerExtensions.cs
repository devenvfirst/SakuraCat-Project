// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Provides extension methods for <see cref="ICameraManager"/>.
    /// </summary>
    public static class CameraManagerExtensions
    {
        /// <summary>
        /// Returns current <see cref="ICameraManager.Resolution"/> aspect ratio (width divided by height).
        /// </summary>
        public static float GetAspectRatio (this ICameraManager mngr) => (float)mngr.Resolution.x / mngr.Resolution.y;
    }
}
