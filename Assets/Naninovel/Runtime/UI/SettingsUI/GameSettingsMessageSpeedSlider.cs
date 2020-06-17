// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.UI
{
    public class GameSettingsMessageSpeedSlider : ScriptableSlider
    {
        protected virtual bool PreviewPrinterAvailable => ObjectUtils.IsValid(previewPrinter) && previewPrinter.isActiveAndEnabled;
        protected GameSettingsPreviewPrinter PreviewPrinter => previewPrinter;

        [SerializeField] private GameSettingsPreviewPrinter previewPrinter = default;

        private ITextPrinterManager printerMngr;

        protected override void Awake ()
        {
            base.Awake();

            printerMngr = Engine.GetService<ITextPrinterManager>();
            UIComponent.value = printerMngr.BaseRevealSpeed;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (PreviewPrinterAvailable)
                previewPrinter.Show();
        }

        protected override void OnDisable ()
        {
            if (PreviewPrinterAvailable)
                previewPrinter.Hide();

            base.OnDisable();
        }

        protected override void OnValueChanged (float value)
        {
            printerMngr.BaseRevealSpeed = value;

            if (PreviewPrinterAvailable)
                previewPrinter.StartPrinting();
        }
    }
}
