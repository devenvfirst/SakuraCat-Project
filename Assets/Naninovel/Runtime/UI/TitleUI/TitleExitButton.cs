// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.UI
{
    public class TitleExitButton : ScriptableButton
    {
        protected override void OnButtonClick ()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                Application.OpenURL("about:blank");
            else Application.Quit();
        }
    }
}
