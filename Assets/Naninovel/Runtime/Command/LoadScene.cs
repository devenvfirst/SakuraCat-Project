// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading;
using UniRx.Async;
using UnityEngine.SceneManagement;

namespace Naninovel.Commands
{
    /// <summary>
    /// Loads a [Unity scene](https://docs.unity3d.com/Manual/CreatingScenes.html) with the provided name.
    /// Don't forget to add the required scenes to the [build settings](https://docs.unity3d.com/Manual/BuildSettings.html) to make them available for loading.
    /// </summary>
    /// <example>
    /// ; Load scene "MyTestScene" in single mode
    /// @loadScene MyTestScene
    /// ; Load scene "MyTestScene" in additive mode
    /// @loadScene MyTestScene additive:true
    /// </example>
    public class LoadScene : Command
    {
        /// <summary>
        /// Name of the scene to load.
        /// </summary>
        [ParameterAlias(NamelessParameterAlias), RequiredParameter]
        public StringParameter SceneName;
        /// <summary>
        /// Whether to load the scene additively, or unload any currently loaded scenes before loading the new one (default).
        /// See the [load scene documentation](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html) for more information.
        /// </summary>
        public BooleanParameter Additive = false;

        public override async UniTask ExecuteAsync (CancellationToken cancellationToken = default)
        {
            await SceneManager.LoadSceneAsync(SceneName, Additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
        }
    }
}
