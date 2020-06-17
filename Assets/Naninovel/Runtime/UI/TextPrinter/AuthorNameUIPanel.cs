// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class AuthorNameUIPanel : AuthorNamePanel
    {
        public override string Text
        {
            get => TextComp ? TextComp.text : null;
            set { if (TextComp) TextComp.text = value ?? string.Empty; }
        }
        public override Color TextColor
        {
            get => TextComp ? TextComp.color : default;
            set { if (TextComp) TextComp.color = value; }
        }

        private Text TextComp => textCompCache ? textCompCache : (textCompCache = GetComponentInChildren<Text>());
        private Text textCompCache;
    }
}
