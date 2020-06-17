// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;

namespace Naninovel
{
    public static class WebGLExtensions
    {
        /// <summary>
        /// Calls FS.syncfs in native js.
        /// </summary>
        [DllImport("__Internal")]
        public static extern void SyncFs ();
    }
}
#endif
