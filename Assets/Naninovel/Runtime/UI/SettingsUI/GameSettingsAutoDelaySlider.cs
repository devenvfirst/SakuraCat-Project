// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.UI
{
    public class GameSettingsAutoDelaySlider : ScriptableSlider
    {
        protected virtual bool PreviewPrinterAvailable => ObjectUtils.IsValid(previewPrinter) && previewPrinter.isActiveAndEnabled;
        protected GameSettingsPreviewPrinter PreviewPrinter => previewPrinter;

        [SerializeField] private GameSettingsPreviewPrinter previewPrinter = default;

        private ITextPrinterManager printerMngr;

        protected override void Awake ()
        {
            base.Awake();

            printerMngr = Engine.GetService<ITextPrinterManager>();
            UIComponent.value = printerMngr.BaseAutoDelay;
        }

        protected override void OnValueChanged (float value)
        {
            printerMngr.BaseAutoDelay = value;

            if (PreviewPrinterAvailable)
                previewPrinter.StartPrinting();
        }
    }
}
