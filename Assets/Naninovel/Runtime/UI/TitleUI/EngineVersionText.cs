// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    [RequireComponent(typeof(Text))]
    public class EngineVersionText : MonoBehaviour
    {
        private void Start ()
        {
            var version = EngineVersion.LoadFromResources();
            GetComponent<Text>().text = $"Naninovel {version.Version}{Environment.NewLine}Build {version.Build}";
        }

    }
}
