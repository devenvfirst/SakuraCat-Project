// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEngine.UI;

namespace Naninovel
{
    public class ScriptableSlider : ScriptableUIControl<Slider>
    {
        public event Action<float> OnSliderValueChanged;

        protected override void BindUIEvents ()
        {
            UIComponent.onValueChanged.AddListener(OnValueChanged);
            UIComponent.onValueChanged.AddListener(InvokeOnSliderValueChanged);
        }

        protected override void UnbindUIEvents ()
        {
            UIComponent.onValueChanged.RemoveListener(OnValueChanged);
            UIComponent.onValueChanged.RemoveListener(InvokeOnSliderValueChanged);
        }

        protected virtual void OnValueChanged (float value) { }

        private void InvokeOnSliderValueChanged (float value)
        {
            if (OnSliderValueChanged != null)
                OnSliderValueChanged.Invoke(value);
        }
    }
}
