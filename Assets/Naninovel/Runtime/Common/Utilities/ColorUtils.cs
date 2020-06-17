// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    public static class ColorUtils
    {
        /// <summary>
        /// Solid white color with zero alpha.
        /// </summary>
        public static Color ClearWhite { get { return clearWhite; } }

        private static Color clearWhite = new Color(1, 1, 1, 0);
    }
}
