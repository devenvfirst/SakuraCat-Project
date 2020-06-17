// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Threading;
using UniRx.Async;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Implementation is able to manage cameras and other systems required for scene rendering.
    /// </summary>
    public interface ICameraManager : IEngineService<CameraConfiguration>
    {
        /// <summary>
        /// Event invoked when view aspect ratio is changed.
        /// </summary>
        event Action<float> OnAspectChanged;

        /// <summary>
        /// Current display resolution, in pixels.
        /// </summary>
        Vector2Int Resolution { get; }
        /// <summary>
        /// Main render camera used by the engine.
        /// </summary>
        Camera Camera { get; }
        /// <summary>
        /// Camera used for UI rendering.
        /// </summary>
        Camera UICamera { get; }
        /// <summary>
        /// Whether render the UI.
        /// </summary>
        bool RenderUI { get; set; }
        /// <summary>
        /// Local camera position offset in units by X and Y axis relative to the initial position set in the configuration.
        /// </summary>
        Vector3 Offset { get; set; }
        /// <summary>
        /// Local camera rotation.
        /// </summary>
        Quaternion Rotation { get; set; }
        /// <summary>
        /// Relatize camera zoom (orthographic size or FOV depending on <see cref="Orthographic"/>), in 0.0 to 1.0 range.
        /// </summary>
        float Zoom { get; set; }
        /// <summary>
        /// Whether the camera should render in orthographic (true) or perspective (false) mode.
        /// </summary>
        bool Orthographic { get; set; }
        /// <summary>
        /// Current camera orthographic size. Use setter of this property instead of the camera's ortho size to respect current zoom level.
        /// </summary>
        float OrthoSize { get; set; }
        /// <summary>
        /// Current rendering quality level (<see cref="QualitySettings"/>) index.
        /// </summary>
        int QualityLevel { get; set; }

        /// <summary>
        /// Activates/disables camera look mode, when player can offset the main camera with input devices 
        /// (eg, by moving a mouse or using gamepad analog stick).
        /// </summary>
        void SetLookMode (bool enabled, Vector2 lookZone, Vector2 lookSpeed, bool gravity);
        /// <summary>
        /// Save current content of the screen to be used as a thumbnail (eg, for save slots).
        /// </summary>
        Texture2D CaptureThumbnail ();
        /// <summary>
        /// Modifies <see cref="Offset"/> over time.
        /// </summary>
        UniTask ChangeOffsetAsync (Vector3 offset, float duration, EasingType easingType = default, CancellationToken cancellationToken = default);
        /// <summary>
        /// Modifies <see cref="Rotation"/> over time.
        /// </summary>
        UniTask ChangeRotationAsync (Quaternion rotation, float duration, EasingType easingType = default, CancellationToken cancellationToken = default);
        /// <summary>
        /// Modifies <see cref="Zoom"/> over time.
        /// </summary>
        UniTask ChangeZoomAsync (float zoom, float duration, EasingType easingType = default, CancellationToken cancellationToken = default);
    } 
}
