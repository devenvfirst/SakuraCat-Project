// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Base class for project-specific configuration assets used to initialize and configure services and other engine systems.
    /// Serialized configuration assets are generated automatically under the engine's data folder and can be edited via Unity project settings menu.
    /// </summary>
    public abstract class Configuration : ScriptableObject { }
}
