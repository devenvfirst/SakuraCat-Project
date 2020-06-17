// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel.UI
{
    public class GameSettingsGraphicsDropdown : ScriptableDropdown
    {
        private ICameraManager cameraManager;

        protected override void Awake ()
        {
            base.Awake();

            cameraManager = Engine.GetService<ICameraManager>();
        }

        protected override void Start ()
        {
            base.Start();

            var options = QualitySettings.names.ToList();
            InitializeOptions(options);
        }

        protected override void OnValueChanged (int value)
        {
            cameraManager.QualityLevel = value;
        }

        private void InitializeOptions (List<string> availableOptions)
        {
            UIComponent.ClearOptions();
            UIComponent.AddOptions(availableOptions);
            UIComponent.value = cameraManager.QualityLevel;
            UIComponent.RefreshShownValue();
        }
    }
}
