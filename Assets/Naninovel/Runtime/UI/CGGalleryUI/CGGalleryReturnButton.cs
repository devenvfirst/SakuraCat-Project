// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel.UI
{
    public class CGGalleryReturnButton : ScriptableButton
    {
        private ICGGalleryUI cgGalleryUI;

        protected override void Awake ()
        {
            base.Awake();

            cgGalleryUI = GetComponentInParent<ICGGalleryUI>();
        }

        protected override void OnButtonClick () => cgGalleryUI.Hide();
    }
}
