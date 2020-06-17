// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents panel hosting name of the current text message author (character) avatar.
    /// </summary>
    public abstract class AuthorNamePanel : MonoBehaviour
    {
        public abstract string Text { get; set; }
        public abstract Color TextColor { get; set; }
    }
}
