// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.UI
{
    public class GameSettingsScreenModeDropdown : ScriptableDropdown
    {
        [ManagedText("DefaultUI")]
        protected static string ExclusiveFullScreen = "Full Screen";
        [ManagedText("DefaultUI")]
        protected static string FullScreenWindow = "Full Screen Window";
        [ManagedText("DefaultUI")]
        protected static string MaximizedWindow = "Maximized Window";
        [ManagedText("DefaultUI")]
        protected static string Windowed = "Windowed";

        private bool allowApplySettings;

        protected override void Start ()
        {
            base.Start();

            InitializeOptions();
            allowApplySettings = true;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            var localeManager = Engine.GetService<ILocalizationManager>();
            if (localeManager != null)
                localeManager.OnLocaleChanged += HandleLocaleChanged;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            var localeManager = Engine.GetService<ILocalizationManager>();
            if (localeManager != null)
                localeManager.OnLocaleChanged -= HandleLocaleChanged;
        }

        protected override void OnValueChanged (int value)
        {
            if (!allowApplySettings) return; // Prevent changing resolution when UI initializes.
            Screen.SetResolution(Screen.width, Screen.height, (FullScreenMode)value, Screen.currentResolution.refreshRate);
        }

        private void InitializeOptions ()
        {
            #if !UNITY_STANDALONE && !UNITY_EDITOR
            transform.parent.gameObject.SetActive(false);
            #else
            var options = new List<string> { ExclusiveFullScreen, FullScreenWindow, MaximizedWindow, Windowed };
            UIComponent.ClearOptions();
            UIComponent.AddOptions(options);
            UIComponent.value = (int)Screen.fullScreenMode;
            UIComponent.RefreshShownValue();
            #endif
        }

        private void HandleLocaleChanged (string locale) => InitializeOptions();
    }
}
